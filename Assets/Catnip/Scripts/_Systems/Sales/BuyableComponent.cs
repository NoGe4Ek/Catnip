using System;
using Catnip.Scripts.Components;
using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Mirror;
using TMPro;
using UnityEngine;
namespace Catnip.Scripts._Systems.Sales {
public class BuyableComponent: MonoBehaviour {
    [SerializeField] public GameObject buyablePrefab;
    [SerializeField] public int cost;
    [SerializeField] public TMP_Text costText;
    
    private GameObject tipObject;

    private void Awake() {
        costText.text = cost + " $";
    }

    public void OnFocus() {
        costText.gameObject.SetActive(true);
        
        if (tipObject != null) return;
        tipObject = Instantiate(G.Instance.tipPrefab, G.Instance.tipsContainer);
        TipUi tipUi = tipObject.GetComponent<TipUi>();
        tipUi.keyText.text = "Left click";
        tipUi.descriptionText.text = "- Buy";
    }
    
    public void OnUnfocus() {
        costText.gameObject.SetActive(false);
        Destroy(tipObject);
    }
    
    public NetworkInteractableComponent Buy(NetworkConnectionToClient sender) {
        SessionManager.Instance.players[sender].balance += cost;
        
        GameObject buyableInstance = Instantiate(buyablePrefab, transform.position, transform.rotation);
        NetworkServer.Spawn(buyableInstance);
        return buyableInstance.GetComponent<NetworkInteractableComponent>();
    }
}
}
