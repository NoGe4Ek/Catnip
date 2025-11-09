using System.Linq;
using Catnip.Scripts._Systems.Backpack;
using Catnip.Scripts._Systems.Customer;
using Catnip.Scripts._Systems.Gardening;
using Catnip.Scripts._Systems.Sales;
using Catnip.Scripts._Systems.Slots;
using Catnip.Scripts.Components;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.DI;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;
using Extensions = Catnip.Scripts.Utils.Extensions;
namespace Catnip.Scripts.Managers {
public class InteractionManager : NetworkBehaviour {
    [SerializeField] public float holdDistance;
    [SerializeField] private float sphereCastRadius = 0.2f;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask storageLayer;
    [SerializeField] private float throwForce = 10f;

    void Update() {
        UpdateOutline();
    }

    NetworkInteractableComponent lastInteractable;
    GameObject lastStorage;
    void UpdateOutline() {
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
        if (Physics.SphereCast(ray, 0.01f, out RaycastHit hit, interactionDistance, interactableLayer | storageLayer)) {
            if (hit.collider.TryGetComponent(out NetworkInteractableComponent interactable)) {
                interactable.OnFocus();
                if (lastInteractable != null && lastInteractable != interactable) lastInteractable.OnUnfocus();
                lastInteractable = interactable;
            } else if ((storageLayer.value & 1 << hit.collider.gameObject.layer) != 0) {
                GameObject storage = hit.collider.gameObject.FindComponentsInChildrenRecursive<SlotsController>(false).First().gameObject;
                storage.SetActive(true);
                if (lastStorage != null && lastStorage != storage) lastStorage.SetActive(false);
                lastStorage = storage;
            }
        } else {
            if (lastInteractable != null) {
                lastInteractable.OnUnfocus();
                lastInteractable = null;
            }
            if (lastStorage != null) {
                lastStorage.SetActive(false);
                lastStorage = null;
            }
        }
    }


    public void InteractPrimary() {
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
        if (PlayerController.LocalPlayer.currentHoldItem == null) {
            // Реализация на SphereCast
            // Extensions.DrawSphereCastDebug(ray, sphereCastRadius, interactionDistance, interactableLayer);
            // Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
            // if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, interactionDistance, interactableLayer)) {
            //     NetworkInteractableComponent interactable = hit.collider.GetComponent<NetworkInteractableComponent>();
            //     CmdTryPickup(interactable);
            // }


            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer | storageLayer)) {
                if (hit.collider.TryGetComponent(out NetworkInteractableComponent interactable) && interactable.isHoldable) {
                    CmdTryPickup(interactable, null);
                } else if (hit.collider.TryGetComponent(out SlotController slotController)) {
                    if (slotController.storeObject != null && slotController.storeObject.TryGetComponent(out NetworkInteractableComponent interactableFromStore)) {
                        CmdTryPickup(interactableFromStore, slotController);
                    }
                }
            }
        } else {
            if (!PlayerController.LocalPlayer.currentHoldItem.TryGetComponent(out StorableComponent storableComponent)) return;
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, storageLayer)) {
                if (!hit.collider.TryGetComponent(out SlotController slotController)) return;
                CmdStoreItem(storableComponent, slotController);
            }
        }
    }

    public void InteractSecondary() {
        if (PlayerController.LocalPlayer.currentHoldItem == null) return;

        NetworkInteractableComponent interactable = PlayerController.LocalPlayer.currentHoldItem.GetComponent<NetworkInteractableComponent>();
        Rigidbody rb = interactable.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        interactable.GetComponent<Collider>().isTrigger = false;
        rb.AddForce(Vector3.zero, ForceMode.Impulse);

        CmdDropItem();
    }

    public void InteractTertiary() {
        if (PlayerController.LocalPlayer.currentHoldItem == null) return;
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());

        if (PlayerController.LocalPlayer.currentHoldItem.TryGetComponent(out IUsable usable)) {
            usable.ClientUse(ray);
        }

        CmdInteractTertiary(ray);
    }

    public void Throw() {
        if (PlayerController.LocalPlayer.currentHoldItem != null) {
            // Рассчитываем силу броска на основе движения игрока
            Vector3 throwDirection = G.Instance.firstPersonCamera.transform.forward;
            Vector3 playerVelocity = PlayerController.LocalPlayer.characterController.velocity;

            // Добавляем скорость игрока к броску
            Vector3 totalThrowForce = (throwDirection * throwForce) + (playerVelocity * 0.5f);

            NetworkInteractableComponent interactable = PlayerController.LocalPlayer.currentHoldItem.GetComponent<NetworkInteractableComponent>();
            Rigidbody rb = interactable.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            interactable.GetComponent<Collider>().isTrigger = false;
            rb.AddForce(totalThrowForce, ForceMode.Impulse);

            CmdThrowItem(totalThrowForce);
        }
    }

    public void Backpack() {
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
        if (PlayerController.LocalPlayer.currentHoldItem == null) {
            if (PlayerController.LocalPlayer.backpack == null) {
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer | storageLayer)) {
                    if (hit.collider.TryGetComponent(out BackpackController backpackController)) {
                        CmdBackpack(backpackController);
                    }
                }
            } else {
                CmdBackpack(null);
            }
        }
    }

    public void Spawn() {
        CmdSpawn();
    }
    
    public void Test() {
        CustomerManager.Instance.RequestPurchase();
    }

    [Command(requiresAuthority = false)]
    void CmdSpawn(NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        BootstrapManager.Instance.SpawnSphere();
    }

    [Command(requiresAuthority = false)]
    void CmdBackpack(BackpackController backpackController, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        if (backpackController == null) {
            // Снимаем рюкзак
            playerController.backpack.GetComponent<BackpackController>().Unequip(playerController.obj.position, playerController.pickUpPoint.position);
            playerController.backpack = null;
        } else {
            // Надеваем рюкзак
            playerController.backpack = backpackController.gameObject;
            backpackController.Equip();
        }
    }

    [Command(requiresAuthority = false)]
    void CmdStoreItem(StorableComponent storableComponent, SlotController slotController, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        playerController.currentHoldItem.GetComponent<NetworkInteractableComponent>().ServerDrop();
        playerController.currentHoldItem = null;

        storableComponent.netIdentity.RemoveClientAuthority();
        slotController.SetContent(storableComponent);
    }

    [Command(requiresAuthority = false)]
    void CmdTryPickup(NetworkInteractableComponent interactable, SlotController slotController, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        if (interactable == null || interactable.isHold || !interactable.isHoldable) return;

        // Пытаемся взять предмет из Store
        if (slotController != null) {
            slotController.RemoveContent();
        }

        // Пробуем присваивать net identity
        interactable.netIdentity.RemoveClientAuthority();
        interactable.netIdentity.AssignClientAuthority(senderOrHost);
        playerController.currentHoldItem = interactable.gameObject;
        // interactable.GetComponent<NetworkTransformHybrid>().syncDirection = SyncDirection.ClientToServer;
        interactable.ServerPickup(playerController.netIdentity);

        // Проверяем, что предмет не занят другим игроком
        // if (interactable.holder == null) {
        //     playerController.currentHoldItem = interactable.gameObject;
        //     interactable.ServerPickup(playerController.netIdentity);
        // }
    }

    [Command(requiresAuthority = false)]
    void CmdThrowItem(Vector3 totalThrowForce, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        // Vector3 throwDirection = transform.forward;
        // Vector3 playerVelocity = playerController.characterController.velocity;
        // Vector3 totalThrowForce = (throwDirection * throwForce) + (playerVelocity * 0.5f);

        NetworkInteractableComponent interactable = playerController.currentHoldItem.GetComponent<NetworkInteractableComponent>();
        // interactable.GetComponent<NetworkTransformHybrid>().syncDirection = SyncDirection.ServerToClient;
        interactable.ServerThrow(totalThrowForce);
        playerController.currentHoldItem = null;

        // interactable.netIdentity.RemoveClientAuthority();
    }

    [Command(requiresAuthority = false)]
    void CmdDropItem(NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        playerController.currentHoldItem.GetComponent<NetworkInteractableComponent>().ServerDrop();
        playerController.currentHoldItem = null;
    }

    [Command(requiresAuthority = false)]
    void CmdInteractTertiary(Ray ray, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        if (playerController.currentHoldItem.TryGetComponent(out IUsable usable)) {
            usable.ServerUse(ray);
        } else if (playerController.currentHoldItem.TryGetComponent(out ISellable sellable)) {
            sellable.Sell(ray, senderOrHost);
        }
    }

    public static InteractionManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}
