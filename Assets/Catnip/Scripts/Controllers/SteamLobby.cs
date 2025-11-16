using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;

namespace Catnip.Scripts.Controllers {
public static class Members {
    public static int GetLobbyMemberCount(CSteamID lobbyId) {
        return lobbyId.IsValid() ? SteamMatchmaking.GetNumLobbyMembers(lobbyId) : 0;
    }

    public static readonly List<CSteamID> LobbyMembers = new List<CSteamID>();

    public static CSteamID GetLobbyMemberByIndex(CSteamID lobbyId, int index) {
        if (lobbyId.IsValid() && index < GetLobbyMemberCount(lobbyId)) {
            return SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, index);
        }

        return CSteamID.Nil;
    }

    public static void UpdateLobbyMembersList(CSteamID lobbyId) {
        LobbyMembers.Clear();

        if (lobbyId.IsValid()) {
            int memberCount = GetLobbyMemberCount(lobbyId);
            for (int i = 0; i < memberCount; i++) {
                CSteamID memberID = GetLobbyMemberByIndex(lobbyId, i);
                LobbyMembers.Add(memberID);
            }
        }
    }
}

[System.Serializable]
public class FriendInfo {
    public CSteamID steamID;
    public string name;
    public EPersonaState state;
}

public static class Friends {
    public static readonly List<FriendInfo> FriendsList = new List<FriendInfo>();

    // Основной метод для загрузки списка друзей
    public static void LoadFriendsList() {
        FriendsList.Clear();

        // Получаем количество друзей
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        for (int i = 0; i < friendCount; i++) {
            CSteamID friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            string friendName = SteamFriends.GetFriendPersonaName(friendID);
            EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendID);

            FriendsList.Add(new FriendInfo {
                steamID = friendID,
                name = friendName,
                state = friendState
            });
        }

        Debug.Log($"Loaded {FriendsList.Count} friends from Steam");
        // PrintFriendsList();
    }

    // Метод для вывода списка друзей в консоль (для отладки)
    private static void PrintFriendsList() {
        foreach (var friend in FriendsList) {
            Debug.Log($"Friend: {friend.name} (State: {friend.state}, ID: {friend.steamID})");
        }
    }

    // Метод для получения списка имен всех друзей
    public static List<string> GetAllFriendNames() {
        List<string> names = new List<string>();
        foreach (var friend in FriendsList) {
            names.Add(friend.name);
        }

        return names;
    }

    // Метод для получения информации о друге по SteamID
    public static FriendInfo GetFriendInfo(CSteamID steamID) {
        return FriendsList.Find(friend => friend.steamID == steamID);
    }

    // Обработчик изменения статуса друга (обновляет список автоматически)
    private static void OnPersonaStateChange(PersonaStateChange_t callback) {
        // Обновляем информацию о друге, если он есть в списке
        for (int i = 0; i < FriendsList.Count; i++) {
            if (FriendsList[i].steamID == (CSteamID)callback.m_ulSteamID) {
                FriendsList[i].name = SteamFriends.GetFriendPersonaName(FriendsList[i].steamID);
                FriendsList[i].state = SteamFriends.GetFriendPersonaState(FriendsList[i].steamID);
                Debug.Log($"Updated friend info: {FriendsList[i].name}");
                break;
            }
        }
    }

    public static FriendInfo FindFriendById(ulong steamId) {
        CSteamID targetId = new CSteamID(steamId);
        return FriendsList.Find(friend => friend.steamID == targetId);
    }

    public static bool InviteFriendToGame(FriendInfo friend) {
        if (!SteamManager.Initialized) {
            Debug.LogError("Steam не инициализирован");
            return false;
        }

        if (!friend.steamID.IsValid()) {
            Debug.LogError("Неверный SteamID друга");
            return false;
        }

        // Отправляем приглашение
        bool success = SteamFriends.InviteUserToGame(friend.steamID, "Присоединяйся к моей игре!");

        if (success) {
            Debug.Log($"Приглашение отправлено другу: {friend.name}");
        } else {
            Debug.LogError("Не удалось отправить приглашение");
        }

        return success;
    }
}

public class SteamLobby : MonoBehaviour {
    public GameObject hostButton = null;

    private NetworkManager networkManager;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HostAddressKey = "HostAddress";

    private CSteamID currentLobbyID;

    private void Start() {
        networkManager = GetComponent<NetworkManager>();

        if (!SteamManager.Initialized) {
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        Friends.LoadFriendsList();
    }

    public void HostLobby() {
        hostButton.SetActive(false);

        SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback) {
        if (callback.m_eResult != EResult.k_EResultOK) {
            hostButton.SetActive(true);
            return;
        }

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey,
            SteamUser.GetSteamID().ToString());
        Members.UpdateLobbyMembersList(currentLobbyID);

        var friend = Friends.FindFriendById(76561198140846746);
        Friends.InviteFriendToGame(friend);
    }

    private static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) {
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
        Members.UpdateLobbyMembersList(currentLobbyID);
    }
}
}