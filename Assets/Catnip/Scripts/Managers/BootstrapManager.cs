using Mirror;
using UnityEngine;
namespace Catnip.Scripts.Managers {
public class BootstrapManager: NetworkBehaviour {
    [SerializeField] public GameObject spherePrefab;
    [SerializeField] public GameObject backpackPrefab;
    
    public override void OnStartServer() {
        base.OnStartServer();
        SpawnSphere();
        
        GameObject backpackInstance = Instantiate(backpackPrefab, new Vector3(0, 0, -1), Quaternion.identity);
        NetworkServer.Spawn(backpackInstance);
    }

    public void SpawnSphere() {
        GameObject sphereInstance = Instantiate(spherePrefab, new Vector3(0, 2, 0), Quaternion.identity);
        NetworkServer.Spawn(sphereInstance);
    }

    public static BootstrapManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}
