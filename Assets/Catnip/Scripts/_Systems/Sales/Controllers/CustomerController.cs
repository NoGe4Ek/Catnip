using Catnip.Scripts._Systems.Customer;
using Catnip.Scripts.Managers;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Sales.Controllers {
public class CustomerController: MonoBehaviour {
    public PurchaseRequest purchaseRequest;
    public void Purchase(GameObject gameObjectToPurchase, NetworkConnectionToClient sender) {
        Destroy(gameObjectToPurchase);
        SessionManager.Instance.players[sender].balance += 10;
        Destroy(gameObject);
        EventManager.TriggerEvent<PurchaseRequest>(EventKey.PurchaseRequestExpire, purchaseRequest);
    } 
}
}
