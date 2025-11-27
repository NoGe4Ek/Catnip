using System.Collections.Generic;
using System.Linq;
using Catnip.Scripts._Systems.Slots;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Mixing {
public class MortarController : MonoBehaviour, ISlotsOwner {
    [SerializeField] public GameObject blendPrefab;
    [SerializeField] public SlotsSettings slotsSettings;
    private SlotsController slotsController;

    public void Awake() {
        slotsController = gameObject.FindComponentsInChildrenRecursive<SlotsController>(false).First();
    }

    public void Mix() {
        var slotWithComponentList = slotsController.slots.Select(slot =>
            (
                Slot: slot,
                Component: slot.storeObject.GetComponentInChildren<MixingComponent>()
            )
        ).ToList();

        if (slotWithComponentList.Count < 2) return;
        
        var first = slotWithComponentList[0];
        var second = slotWithComponentList[1];
        Mixing.Mix newMix = new Mixing.Mix {
            mixBase = first.Component.mix.mixBase != Mixing.MixBase.Empty
                ? first.Component.mix.mixBase
                : second.Component.mix.mixBase,
            mixComponents = new List<Mixing.MixComponent>(first.Component.mix.mixComponents
                .Concat(second.Component.mix.mixComponents))
        };

        first.Slot.Clear();
        second.Slot.Clear();

        GameObject blendInstance = Instantiate(blendPrefab);
        MixingComponent newMixingComponent = blendInstance.GetComponent<MixingComponent>();
        newMixingComponent.mix = newMix;
        StorableComponent newStorableComponent = blendInstance.GetComponent<StorableComponent>();
        first.Slot.SetContent(newStorableComponent);
        blendInstance.SetActive(false);
    }

    public SlotsSettings GetSlotsSettings() {
        return slotsSettings;
    }

    public bool GetSlotAvailability(SlotPosition position, GameObject storeObject) {
        if (position.width == 0 && position.height == 0) {
            if (!storeObject.TryGetComponent(out MixingComponent mixingComponent)) return false;
            if (mixingComponent.mix.mixBase == Mixing.MixBase.Empty) return false;
        }

        return true;
    }
}
}