using Catnip.Scripts._Systems.Slots;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Backpack {
public class BackpackController: NetworkBehaviour, ISlotsOwner {
    [SerializeField] public SlotsSettings slotsSettings;
    
    [SyncVar(hook = nameof(OnIsEquippedChanged))] public bool isEquipped;
    private void OnIsEquippedChanged(bool oldValue, bool newValue) {
        gameObject.SetActive(!newValue);
    }
    
    public void Equip() {
        isEquipped = true;
    }
    
    public void Unequip(Vector3 playerPosition, Vector3 backpackPosition) {
        RpcSetPositionBeforeActive(playerPosition, backpackPosition);
        transform.position = backpackPosition;
        transform.LookAt(playerPosition);
        isEquipped = false;
    }

    [ClientRpc]
    private void RpcSetPositionBeforeActive(Vector3 playerPosition, Vector3 backpackPosition) {
        transform.position = backpackPosition;
        transform.LookAt(playerPosition);
    }

    public SlotsSettings GetSlotsSettings() {
        return slotsSettings;
    }
    public bool GetSlotAvailability(SlotPosition position, GameObject storeObject) {
        return true;
    }
}
}
