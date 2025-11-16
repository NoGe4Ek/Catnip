using Catnip.Scripts._Systems.Sales.Controllers;
using Catnip.Scripts.DI;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Sales {
public class CatnipController : MonoBehaviour, ISellable {

    public void Sell(Ray ray, NetworkConnectionToClient sender) {
        Debug.DrawRay(ray.origin, ray.direction, Color.red);
        if (Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 3f, G.Instance.holdableLayer)) {
            if (hit.collider.TryGetComponent(out CustomerController customerController)) {
                customerController.Purchase(gameObject, sender);
            }
        }
    }
}
}
