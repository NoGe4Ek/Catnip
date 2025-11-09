using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Catnip.Scripts.Controllers;
using Mirror;
using TMPro;
using UnityEngine;
using Random = System.Random;

namespace Catnip.Scripts.Managers {
[Serializable]
public enum GameState {
    WaitingForPlayers,
    Setup,
    InProgress,
    Paused,
    Finished
}

public class SessionManager : NetworkBehaviour {
    public static event Action<int> OnTimeTick;
    [SerializeField] public TMP_Text gameTimerText;

    [SyncVar(hook = nameof(OnGameTimerChanged))]
    public int durationFromStart; // seconds

    private void OnGameTimerChanged(int oldGameTimer, int newGameTimer) {
        OnTimeTick?.Invoke(newGameTimer);
    }

    [SyncVar(hook = nameof(OnStateChange))]
    public GameState state;

    private void OnStateChange(GameState oldState, GameState newState) {
        switch (newState) {
            case GameState.WaitingForPlayers:
                break;
            case GameState.Setup:
                break;
            case GameState.InProgress:
                break;
            // case GameState.Paused:
            //     break;
            // case GameState.Finished:
            //     break;
        }
    }

    private Task gameTimerCoroutine;
    private float gameTime;

    // List of players (server only).
    // NetworkConnectionToClient available only on server 
    public readonly Dictionary<NetworkConnectionToClient, PlayerController> players = new();

    // List of players (client and server).
    // Store PlayerController for each integer id (order number of player connection)
    // P.S. Known on client side
    public readonly Dictionary<int, PlayerController> knownPlayers = new();

    public void AddPlayer(NetworkConnectionToClient conn, PlayerController player) {
        // Save player connection to controller (for server needs)
        players.Add(conn, player);

        // Save player id to controller (for client needs)
        // Trigger AddKnownPlayer on clients
        var playerId = players.Count - 1;
        player.playerId = playerId;
        knownPlayers[playerId] = player;
    }

    // Find all Controllers on client scene and get one with player's id
    // [Client]
    public void AddKnownPlayer(int playerId) {
        var playerController =
            FindObjectsByType<PlayerController>(
                    findObjectsInactive: FindObjectsInactive.Exclude,
                    sortMode: FindObjectsSortMode.None
                )
                .FirstOrDefault(player => player.playerId == playerId);
        knownPlayers[playerId] = playerController;

        EventManager.TriggerEvent<PlayerController>(EventKey.NewPlayerConnected, playerController);
    }

    public void RemovePlayer(NetworkConnectionToClient conn) {
        var player = players[conn];
        knownPlayers.Remove(player.playerId);

        players.Remove(conn);
    }

    private void Start() {
        EventManager.AddListener<PlayerController>(EventKey.LocalPlayerReady, controller => {
            for (var i = 0; i < controller.playerId; i++) {
                if (!knownPlayers.ContainsKey(i))
                    AddKnownPlayer(i);
            }
        });
    }

    private void Update() {
        if (!isServer) return;
        if (state == GameState.Setup) {
            SetupGame();
        }

        if (state == GameState.InProgress && (gameTimerCoroutine == null || !gameTimerCoroutine.Running)) {
            gameTimerCoroutine = new Task(UpdateGameTimer());
        }
    }

    private IEnumerator UpdateGameTimer() {
        var lastUpdateTime = Time.time;

        while (state == GameState.InProgress) {
            var currentTime = Time.time;
            gameTime += currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            var minutes = Mathf.FloorToInt(gameTime / 60);
            var seconds = Mathf.FloorToInt(gameTime % 60);

            gameTimerText.text = $"{minutes:00}:{seconds:00}";

            durationFromStart = (int)gameTime;
            
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SetupGame() {
        Debug.Log("SetupGame: players - " + players.Values.Count);

        state = GameState.InProgress;
    }

    public static SessionManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        state = GameState.Setup;
    }
}
}