using System;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Gardening.Controllers {
public class CatnipBashController : NetworkBehaviour {
    [SerializeField] private GameObject catnipPrefab;

    private GameObject root;

    private void Awake() {
        root = gameObject.transform.parent.gameObject;
    }

    public void Cut() {
        CutInternal();
        RpcCut();
        
        root.FindComponentInParentRecursive<PotController>().growState = GrowState.Empty;
        for (int i = 0; i < 3; i++) {
            GameObject catnipInstance = Instantiate(catnipPrefab, new Vector3(transform.position.x, 3f, transform.position.z), Quaternion.identity);
            NetworkServer.Spawn(catnipInstance);
        }
    }

    [ClientRpc]
    public void RpcCut() {
        CutInternal();
    }

    private void CutInternal() {
        root.SetActive(false);
    }
}
}
