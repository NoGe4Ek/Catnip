using System.Collections.Generic;
using System.Linq;
using Catnip.Scripts._Systems.Gardening;
using Catnip.Scripts._Systems.Slots;
using Catnip.Scripts.DI;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Mixing {
public class PestleController : MonoBehaviour, IUsable {
    public void ClientUse(Ray ray) {
        // unused
    }

    public void ServerUse(Ray ray) {
        Debug.DrawRay(ray.origin, ray.direction, Color.red);
        
        if (!Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 3f, G.Instance.holdableLayer)) return;
        if (!hit.collider.TryGetComponent(out MortarController mortarController)) return;
        
        mortarController.Mix();
    }
}
}