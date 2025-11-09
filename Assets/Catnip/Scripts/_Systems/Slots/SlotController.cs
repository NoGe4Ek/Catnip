using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Slots {
public class SlotController : NetworkBehaviour {
    [SyncVar(hook = nameof(OnStoreObjectChange))] public GameObject storeObject;
    private void OnStoreObjectChange(GameObject oldValue, GameObject newValue) {
        if (newValue == null) {
            oldValue.SetActive(true);
        } else {
            newValue.SetActive(false);
        }
    }

    [SyncVar(hook = nameof(OnStorePreviewObjectChange))] public GameObject storePreviewObject;
    private void OnStorePreviewObjectChange(GameObject oldValue, GameObject newValue) {
        if (newValue == null) return;
        // Нельзя объединить с инстанциированием, иначе сервер не сможет заспавнить этот объект внутри родителя с NetworkIdentity.
        // А вот изменить ему родителя и позицию после спавна уже можно
        newValue.transform.SetParent(transform);
        newValue.transform.position = transform.position;
    }
    
    [Server]
    public void SetContent(StorableComponent storable) {
        storeObject = storable.gameObject;
        storable.transform.position = transform.position;
        storable.gameObject.SetActive(false);
        GameObject storePreview = Instantiate(storable.storePreviewPrefab);
        NetworkServer.Spawn(storePreview);
        storePreviewObject = storePreview;
    }

    [Server]
    public void RemoveContent() {
        NetworkServer.Destroy(storePreviewObject);
        storeObject.transform.position = transform.position;
        storeObject.gameObject.SetActive(true);
        storeObject = null;
        storePreviewObject = null;
    }
}
}
