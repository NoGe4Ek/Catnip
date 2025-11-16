using Catnip.Scripts._Systems.Interaction.Components;
using TMPro;
using UnityEngine;
namespace Catnip.Scripts.Controllers {
public class BasketballCounterController : MonoBehaviour {
    [SerializeField] public TMP_Text counter;

    void Start() {
        counter.text = "0";
    }

    private void OnTriggerEnter(Collider other) {
        if (!other.TryGetComponent<NetworkHoldableComponent>(out var holdable)) return;
        if (!holdable.TryGetComponent<Rigidbody>(out var rb)) return;
        if (rb.isKinematic) return;
        counter.text = (int.Parse(counter.text) + 1).ToString();
    }
}
}
