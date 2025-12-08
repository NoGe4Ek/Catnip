using System.Collections;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Stamina {
public class RagdollManager : NetworkBehaviour {
    public bool isKnockout;

    public void TryKnockout() {
        if (isKnockout) return;
        isKnockout = true;
        CmdKnockout();
        G.Instance.movementManager.SetThirdPersonView();
    }
    
    public void TryKnockin() {
        if (!isKnockout) return;
        isKnockout = false;
        CmdKnockin();
        G.Instance.movementManager.SetFirstPersonView();
    }
    
    [Command(requiresAuthority = false)]
    void CmdKnockout(NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        playerController.isKnockout = true;
    }
    
    [Command(requiresAuthority = false)]
    void CmdKnockin(NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        playerController.isKnockout = false;
    }

    public static RagdollManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}