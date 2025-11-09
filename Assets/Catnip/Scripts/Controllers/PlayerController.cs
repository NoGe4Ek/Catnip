using System;
using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Catnip.Scripts.Models;
using Mirror;
using TMPro;
using UnityEngine;
namespace Catnip.Scripts.Controllers {
public class PlayerController : NetworkBehaviour {
    [SyncVar(hook = nameof(OnPlayerIdChange))]
    public int playerId;
    public void OnPlayerIdChange(int oldPlayerId, int newPlayerId) {
        if (newPlayerId == -1) return;
        SessionManager.Instance.AddKnownPlayer(newPlayerId);
    }

    [SyncVar] public GameObject currentHoldItem;
    [SyncVar(hook = nameof(OnBackpackChange))] public GameObject backpack;
    private void OnBackpackChange(GameObject oldValue, GameObject newValue) {
        backpackMesh.SetActive(newValue != null);
    }
    [SerializeField] public GameObject backpackMesh;
    [SerializeField] public Transform pickUpPoint;
    
    public Transform orientation;
    public Transform obj;
    // public Transform player;
    public float movementSpeed = 10.0f, rotationSpeed = 5.0f, jumpForce = 10.0f, gravity = -30f;
    public GameObject firstPersonFollow, thirdPersonFollow;
    public CharacterController characterController;
    public PersonView personView = PersonView.First;
    public Animator animator;
    
    [SyncVar(hook = nameof(OnHolderChanged))] public int balance;
    private void OnHolderChanged(int oldValue, int newValue) {
        G.Instance.moneyBalanceText.text = newValue.ToString();
    }

    public static PlayerController LocalPlayer;
    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();
        G.Instance.thirdPersonCamera.gameObject.SetActive(false);
        firstPersonFollow.SetActive(true);
        thirdPersonFollow.SetActive(false);
        gameObject.SetActive(true);
        G.Instance.movementManager.playerController = this;
        G.Instance.movementManager.gameObject.SetActive(true);
        G.Instance.inputManager.gameObject.SetActive(true);

        LocalPlayer = this;
        EventManager.TriggerEvent(EventKey.LocalPlayerReady, LocalPlayer);
    }

    public void Awake() {
        // Должно быть проинициализированно перед изменением в хуке, чтобы первое первое изменение применилось
        playerId = -1;
    }
}
}
