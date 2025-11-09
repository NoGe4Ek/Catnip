using UnityEngine;
using UnityEngine.InputSystem;
namespace Catnip.Scripts._Systems.Gardening {
public class SprayController : MonoBehaviour, IUsable {
    [SerializeField] private LayerMask interactableLayer;

    public void ClientUse(Ray ray) {
        // unused
    }

    public void ServerUse(Ray ray) {
        Debug.DrawRay(ray.origin, ray.direction, Color.red);
        
        if (!Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 3f)) return;
        if (!hit.collider.TryGetComponent(out PotController potController)) return;
        
        potController.AddWater();
    }
}
}
