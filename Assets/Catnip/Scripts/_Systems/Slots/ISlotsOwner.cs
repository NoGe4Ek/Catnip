using System;
using UnityEngine;

namespace Catnip.Scripts._Systems.Slots {
[Serializable]
public class SlotsSettings {
    public int width;
    public int height;

    public SlotsSettings(int width, int height) {
        this.width = width;
        this.height = height;
    }

    public SlotsSettings() { }
}

public interface ISlotsOwner {
    public SlotsSettings GetSlotsSettings();
    public bool GetSlotAvailability(SlotPosition position, GameObject storeObject);
}
}