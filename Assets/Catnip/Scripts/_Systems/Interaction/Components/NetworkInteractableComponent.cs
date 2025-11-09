using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts.Components {
[RequireComponent(typeof(Outline))]
public class NetworkInteractableComponent : NetworkBehaviour {
    [SerializeField] public bool isHoldable = true;

    [SyncVar(hook = nameof(OnIsHoldChanged))]
    public bool isHold;

    [SyncVar(hook = nameof(OnHolderChanged))]
    public NetworkIdentity holder;

    Rigidbody rb;
    Outline outline;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        outline = GetComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 6;
        outline.enabled = false;
    }

    public void OnFocus() {
        if (isHold) return;
        outline.enabled = true;
    }

    public void OnUnfocus() {
        outline.enabled = false;
    }

    void Update() {
        if (!isOwned || !isHold) return;
        Transform cameraTransform = G.Instance.firstPersonCamera.transform;
        Vector3 holdPoint = cameraTransform.position + cameraTransform.forward * InteractionManager.Instance.holdDistance;

        transform.position = Vector3.Lerp(
            transform.position,
            holdPoint,
            Time.deltaTime * 10f
        );
        // Debug.Log("NetworkInteractableComponent UpdateHoldItemPosition");

        // if (isClient && !isHost) return;
        // if (!isHold) return;
        // var newPosition = holder.GetComponent<NetworkInteractionController>().currentHoldPosition;
        // UpdateHoldItemPosition(newPosition);
        // Debug.Log("NetworkInteractableComponent UpdateHoldItemPosition");
    }

    void UpdateHoldItemPosition(Vector3 holdPoint) {
        // Плавное перемещение предмета в точку удержания

        transform.position = Vector3.Lerp(
            transform.position,
            holdPoint,
            Time.deltaTime * 10f
        );

        // Поворачиваем предмет так, чтобы он смотрел в камеру
        // transform.rotation = Quaternion.Lerp(
        //     transform.rotation,
        //     G.Instance.firstPersonCamera.transform.rotation,
        //     Time.deltaTime * 8f
        // );
    }

    // Серверный метод для поднятия предмета
    [Server]
    public void ServerPickup(NetworkIdentity player) {
        isHold = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
        outline.enabled = false;
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
        gameObject.layer = LayerMask.NameToLayer("Interactable");
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

    // Хук для синхронизации состояния "удерживается"
    private void OnIsHoldChanged(bool oldValue, bool newValue) {
        gameObject.layer = LayerMask.NameToLayer(newValue ? "Default" : "Interactable");
        GetComponent<Collider>().isTrigger = newValue;

        rb.isKinematic = newValue;
    }

    // Хук для синхронизации держащего игрока
    private void OnHolderChanged(NetworkIdentity oldHolder, NetworkIdentity newHolder) {
        transform.SetParent(newHolder != null ? newHolder.transform : null);
    }
}
}
