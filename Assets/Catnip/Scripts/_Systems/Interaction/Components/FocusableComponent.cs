using UnityEngine;

namespace Catnip.Scripts._Systems.Interaction.Components {
[RequireComponent(typeof(Outline))]
public class FocusableComponent: MonoBehaviour {
    private Outline outline;

    private void Awake() {
        outline = GetComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 6;
        outline.enabled = false;
    }
    
    public void OnFocus() {
        if (TryGetComponent(out NetworkHoldableComponent holdable) && holdable.isHold) return;
        outline.enabled = true;
    }

    public void OnUnfocus() {
        outline.enabled = false;
    }
}
}