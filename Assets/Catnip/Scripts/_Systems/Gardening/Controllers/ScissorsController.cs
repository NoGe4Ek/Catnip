using Catnip.Scripts._Systems.Gardening.Controllers;
using UnityEngine;
namespace Catnip.Scripts._Systems.Gardening {
public class ScissorsController : MonoBehaviour, IUsable {
    public void ClientUse(Ray ray) {
        // unused
    }
    
    public void ServerUse(Ray ray) {
        Debug.DrawRay(ray.origin, ray.direction, Color.red);
        
        if (!Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 3f)) return;
        if (!hit.collider.TryGetComponent(out CatnipBashController catnipBashController)) return;
        
        catnipBashController.Cut();
        
        Debug.Log("ScissorsController Use() - " + hit.collider.gameObject.name);
    }
}
}
