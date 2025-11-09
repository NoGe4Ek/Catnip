using System.Collections;
using System.Collections.Generic;
using Catnip.Scripts._Systems.Sales.Controllers;
using Catnip.Scripts.Managers;
using Catnip.Scripts.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace Catnip.Scripts._Systems.Customer {
public class PurchaseRequest {
    public int requestTime;
    public int waitingTime;
    public bool isComplete;

    public int ExpirationTime() {
        return requestTime + waitingTime;
    }

    public bool IsExpired() {
        if (isComplete) return false;
        return SessionManager.Instance.durationFromStart >= ExpirationTime();
    }
}

public class CustomerManager : MonoBehaviour {
    [SerializeField] public Transform requestsUiParent;
    [SerializeField] public GameObject purchaseRequestUiPrefab;
    [SerializeField] public GameObject customerPrefab;
    [SerializeField] public List<Transform> points;
    public List<PurchaseRequest> requests = new();
    public Dictionary<PurchaseRequest, Task> requestExpireTasks = new();

    public void RequestPurchase() {
        PurchaseRequest purchaseRequest = new PurchaseRequest {
            requestTime = SessionManager.Instance.durationFromStart,
            waitingTime = 10
        };
        requests.Add(purchaseRequest);

        GameObject purchaseRequestUiInstance = Instantiate(purchaseRequestUiPrefab, requestsUiParent);
        PurchaseRequestUi purchaseRequestUi = purchaseRequestUiInstance.GetComponent<PurchaseRequestUi>();
        purchaseRequestUi.Init(purchaseRequest);
        
        GameObject customerInstance = Instantiate(customerPrefab, points.GetRandom().position, Quaternion.identity);
        customerInstance.GetComponent<NavMeshAgent>().SetDestination(points.GetRandom().position);
        customerInstance.GetComponent<CustomerController>().purchaseRequest = purchaseRequest;
        
        requestExpireTasks[purchaseRequest] = new Task(SubscribeToExpire(purchaseRequest, customerInstance));
    }

    private IEnumerator SubscribeToExpire(PurchaseRequest purchaseRequest, GameObject customerObject) {
        while (!purchaseRequest.IsExpired()) {
            int secondsRemain = purchaseRequest.ExpirationTime() - SessionManager.Instance.durationFromStart;
            EventManager.TriggerEvent<PurchaseRequest, int>(EventKey.PurchaseRequestTick, purchaseRequest, secondsRemain);
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(customerObject);
        EventManager.TriggerEvent<PurchaseRequest>(EventKey.PurchaseRequestExpire, purchaseRequest);
    }

    public static CustomerManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}