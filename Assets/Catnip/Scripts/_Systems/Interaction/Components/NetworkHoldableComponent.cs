using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Interaction.Components {
public class NetworkHoldableComponent : NetworkBehaviour {
    [SyncVar(hook = nameof(OnIsHoldChanged))]
    public bool isHold;

    [SyncVar(hook = nameof(OnHolderChanged))]
    public NetworkIdentity holder;

    private Rigidbody rb;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        if (!isOwned || !isHold) return;
        Transform cameraTransform = G.Instance.mainCamera.transform;
        Vector3 holdPoint = cameraTransform.position +
                            cameraTransform.forward * InteractionManager.Instance.holdDistance;

        transform.position = Vector3.Lerp(
            transform.position,
            holdPoint,
            Time.deltaTime * 10f
        );
    }

    // Серверный метод для поднятия предмета
    [Server]
    public void ServerPickup(NetworkIdentity player) {
        isHold = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
        holder = player;
        rb.isKinematic = true;
        GetComponent<Collider>().isTrigger = true;

        // Перемещаем объект к игроку на сервере
        transform.SetParent(player.transform);
    }

    // Серверный метод для броска предмета
    [Server]
    public void ServerThrow(Vector3 force) {
        isHold = false;
        gameObject.layer = G.Instance.holdableLayer.ToLayer();
        holder = null;
        transform.SetParent(null);
        // rb.isKinematic = false;
        // GetComponent<Collider>().isTrigger = false;
        // rb.AddForce(force, ForceMode.Impulse);
    }

    [Server]
    public void ServerDrop() {
        ServerThrow(Vector3.zero);
    }
    
    #region Hooks

    // Хук для синхронизации состояния "удерживается"
    private void OnIsHoldChanged(bool oldValue, bool newValue) {
        gameObject.layer = newValue ? G.Instance.defaultLayer.ToLayer() : G.Instance.holdableLayer.ToLayer();
        GetComponent<Collider>().isTrigger = newValue;

        rb.isKinematic = newValue;
    }

    // Хук для синхронизации держащего игрока
    private void OnHolderChanged(NetworkIdentity oldHolder, NetworkIdentity newHolder) {
        transform.SetParent(newHolder != null ? newHolder.transform : null);
    }

    #endregion
}
}