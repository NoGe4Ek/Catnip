using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
public class SteamLobby : MonoBehaviour {

    public GameObject hostButton = null;


    private NetworkManager networkManager;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HostAddressKey = "HostAddress";

    private CSteamID currentLobbyID;
    public int GetLobbyMemberCount() {
        return currentLobbyID.IsValid() ? SteamMatchmaking.GetNumLobbyMembers(currentLobbyID) : 0;
    }
    public static readonly List<CSteamID> LobbyMembers = new List<CSteamID>();
    public CSteamID GetLobbyMemberByIndex(int index) {
        if (currentLobbyID.IsValid() && index < GetLobbyMemberCount()) {
            return SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, index);
        }
        return CSteamID.Nil;
    }
    public string GetPlayerName(CSteamID steamID) {
        return SteamFriends.GetFriendPersonaName(steamID);
    }
    private void UpdateLobbyMembersList() {
        LobbyMembers.Clear();

        if (currentLobbyID.IsValid()) {
            int memberCount = GetLobbyMemberCount();
            for (int i = 0; i < memberCount; i++) {
                CSteamID memberID = GetLobbyMemberByIndex(i);
                LobbyMembers.Add(memberID);
            }
        }
    }

    private void Start() {
        networkManager = GetComponent<NetworkManager>();

        if (!SteamManager.Initialized) { return; }


        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }



    public void HostLobby() {

        hostButton.SetActive(false);

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);

    }



    private void OnLobbyCreated(LobbyCreated_t callback) {
        if (callback.m_eResult != EResult.k_EResultOK) {
            hostButton.SetActive(true);
            return;
        }


        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
        UpdateLobbyMembersList();
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback) {

        if (NetworkServer.active) {
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        hostButton.SetActive(false);
        UpdateLobbyMembersList();
    }


}
