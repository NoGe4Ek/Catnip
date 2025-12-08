using System;
using Catnip.Scripts.DI;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Slots {
[Serializable]
public class SlotPosition {
    public int width;
    public int height;

    public SlotPosition(int width, int height) {
        this.width = width;
        this.height = height;
    }

    public SlotPosition() { }
}

public class SlotController : NetworkBehaviour {
    [SyncVar] public SlotsController slotsController;

    [SyncVar(hook = nameof(OnSlotPositionChange))]
    public SlotPosition slotPosition;

    private void OnSlotPositionChange(SlotPosition oldValue, SlotPosition newValue) {
        transform.SetParent(slotsController.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.layer = G.Instance.storageLayer.ToLayer();
        transform.localPosition =
            new Vector3(
                slotsController.slotStartOffset + newValue.width * slotsController.slotCommonOffset +
                (-slotsController.backgroundObject.GetLocalRenderCenter().x * slotsController.slotsSettings.width),
                -slotsController.slotStartOffset + newValue.height * -slotsController.slotCommonOffset,
                slotsController.slotZOffset
            );
    }

    [SyncVar(hook = nameof(OnStoreObjectChange))]
    public GameObject storeObject;

    private void OnStoreObjectChange(GameObject oldValue, GameObject newValue) {
        if (newValue == null) {
            oldValue.SetActive(true);
        } else {
            newValue.SetActive(false);
        }
    }

    [SyncVar(hook = nameof(OnStorePreviewObjectChange))]
    public GameObject storePreviewObject;

    private void OnStorePreviewObjectChange(GameObject oldValue, GameObject newValue) {
        if (newValue == null) return;
        // Нельзя объединить с инстанциированием, иначе сервер не сможет заспавнить этот объект внутри родителя с NetworkIdentity.
        // А вот изменить ему родителя и позицию после спавна уже можно
        newValue.transform.SetParent(transform);
        newValue.transform.localPosition = Vector3.zero;
        newValue.transform.localRotation = Quaternion.identity;
    }

    [Server]
    public void SetContent(StorableComponent storable) {
        if (!IsSetContentAvailable(slotPosition, storable.gameObject)) {
            return;
        }

        storeObject = storable.gameObject;
        storable.transform.position = transform.position;
        storable.gameObject.SetActive(false);
        GameObject storePreview = Instantiate(storable.storePreviewPrefab);
        NetworkServer.Spawn(storePreview);
        storePreviewObject = storePreview;
    }

    private bool IsSetContentAvailable(SlotPosition currentSlotPosition, GameObject storable) {
        ISlotsOwner slotsOwner = gameObject.FindComponentInParentRecursive<ISlotsOwner>();
        if (slotsOwner == null) {
            Debug.LogError($"Slot {gameObject.name} has no slots owner");
            return false;
        }

        return slotsOwner.GetSlotAvailability(currentSlotPosition, storable);
    }

    [Server]
    public void RemoveContent() {
        NetworkServer.Destroy(storePreviewObject);
        storeObject.transform.position = transform.position;
        storeObject.gameObject.SetActive(true);
        storeObject = null;
        storePreviewObject = null;
    }

    [Server]
    public void Clear() {
        NetworkServer.Destroy(storePreviewObject);
        NetworkServer.Destroy(storeObject);
    }
}
}