using System.Collections;
using Catnip.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Catnip.Scripts._Systems.Customer {
public class PurchaseRequestUi : MonoBehaviour {
    [SerializeField] public RawImage customerImage;
    [SerializeField] public TMP_Text customerName;
    [SerializeField] public TMP_Text expireTimer;
    private Task timerTask;

    public void Init(PurchaseRequest newPurchaseRequest) {
        
        EventManager.AddListener<PurchaseRequest, int>(EventKey.PurchaseRequestTick, (purchaseRequest, secondsRemain) => {
            if (newPurchaseRequest == purchaseRequest) {
                expireTimer.text = secondsRemain.ToString();
            }
        });
        
        EventManager.AddListener<PurchaseRequest>(EventKey.PurchaseRequestExpire, purchaseRequest => {
            if (newPurchaseRequest == purchaseRequest) {
                Destroy(gameObject);
            }
        });
    }
}
}