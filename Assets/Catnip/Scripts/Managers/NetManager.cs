using Catnip.Scripts.Controllers;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts.Managers {
public class NetManager : NetworkManager {
    // [Server]
    public override void OnServerAddPlayer(NetworkConnectionToClient conn) {
        Debug.Log("New player connected: " + conn);

        var player = Instantiate(playerPrefab);
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);

        var playerController = player.GetComponent<PlayerController>();
        SessionManager.Instance.AddPlayer(conn, playerController);
    }

    // [Server]
    public override void OnServerDisconnect(NetworkConnectionToClient conn) {
        base.OnServerDisconnect(conn);
        
        SessionManager.Instance.RemovePlayer(conn);
    }
}
}
