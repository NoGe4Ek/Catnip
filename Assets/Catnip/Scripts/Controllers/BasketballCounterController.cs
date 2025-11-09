using Catnip.Scripts.Components;
using TMPro;
using UnityEngine;
namespace Catnip.Scripts.Controllers {
public class BasketballCounterController : MonoBehaviour {
    [SerializeField] public TMP_Text counter;

    void Start() {
        counter.text = "0";
    }

    private void OnTriggerEnter(Collider other) {
        if (!other.TryGetComponent<NetworkInteractableComponent>(out var interactable)) return;
        if (!interactable.TryGetComponent<Rigidbody>(out var rb)) return;
        if (rb.isKinematic) return;
        counter.text = (int.Parse(counter.text) + 1).ToString();
    }
}
}
