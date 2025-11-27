using System.Linq;
using Catnip.Scripts._Systems.Backpack;
using Catnip.Scripts._Systems.Customer;
using Catnip.Scripts._Systems.Gardening;
using Catnip.Scripts._Systems.Interaction.Components;
using Catnip.Scripts._Systems.Sales;
using Catnip.Scripts._Systems.Slots;
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
    [SerializeField] private float throwForce = 10f;

    void FixedUpdate() {
        UpdateFocus();
    }

    FocusableComponent lastFocusable;
    SlotsController lastStorage;
    BuyableComponent lastBuyable;

    void UpdateFocus() {
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
        var layer = G.Instance.holdableLayer | G.Instance.storageZoneLayer | G.Instance.storageLayer;

        if (Physics.SphereCast(ray, 0.01f, out RaycastHit hit, interactionDistance, layer)) {
            // Обработка фокуса по объекту, доступного к покупке
            if (hit.collider.TryGetComponent(out BuyableComponent buyable)) {
                if (hit.collider.TryGetComponent(out FocusableComponent focusable)) {
                    buyable.OnFocus();
                    focusable.OnFocus();
                    UpdateUnfocus(buyable: buyable, focusable: focusable);
                    lastBuyable = buyable;
                    lastFocusable = focusable;
                }
            }
            // Обработка фокуса по объекту, доступного к выделению Outline
            else if (hit.collider.TryGetComponent(out FocusableComponent focusable)) {
                focusable.OnFocus();
                UpdateUnfocus(focusable: focusable);
                lastFocusable = focusable;
            }
            // Обработка фокуса по объекту, доступного к отображению системы слотов
            else if (hit.IsInLayer(G.Instance.storageZoneLayer.value | G.Instance.storageLayer)) {
                SlotsController storage =
                    hit.collider.gameObject.FindComponentsInChildrenRecursive<SlotsController>(false).First();
                
                UpdateUnfocus(storage: storage);
                if (!storage.CanShow()) return;
                storage.gameObject.SetActive(true);
                lastStorage = storage;
            } else {
                UpdateUnfocus();
            }
        } else {
            UpdateUnfocus();
        }
    }

    private void UpdateUnfocus(FocusableComponent focusable = null, SlotsController storage = null,
        BuyableComponent buyable = null) {
        if ((lastFocusable != null && focusable == null) ||
            (lastFocusable != null && focusable != null && lastFocusable != focusable)) {
            lastFocusable.OnUnfocus();
            lastFocusable = null;
        }

        if (((lastStorage != null && storage == null) ||
             (lastStorage != null && storage != null && lastStorage != storage))
            && lastStorage.IsSlotsEmpty()
           ) {
            lastStorage.gameObject.SetActive(false);
            lastStorage = null;
        }

        if ((lastBuyable != null && buyable == null) ||
            (lastBuyable != null && buyable != null && lastBuyable != buyable)) {
            lastBuyable.OnUnfocus();
            lastBuyable = null;
        }
    }


    public void InteractPrimary() {
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
        if (PlayerController.LocalPlayer.currentHoldItem == null) {
            // Реализация на SphereCast
            // Extensions.DrawSphereCastDebug(ray, sphereCastRadius, interactionDistance, holdableLayer);
            // Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
            // if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, interactionDistance, holdableLayer)) {
            //     NetworkHoldableComponent holdable = hit.collider.GetComponent<NetworkHoldableComponent>();
            //     CmdTryPickup(holdable);
            // }


            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance,
                    G.Instance.holdableLayer | G.Instance.storageLayer)) {
                if (hit.collider.TryGetComponent(out NetworkHoldableComponent holdable)) {
                    CmdTryPickup(holdable, null);
                } else if (hit.collider.TryGetComponent(out SlotController slotController)) {
                    if (slotController.storeObject != null &&
                        slotController.storeObject.TryGetComponent(out NetworkHoldableComponent holdableFromStore)) {
                        CmdTryPickup(holdableFromStore, slotController);
                    }
                } else if (hit.collider.TryGetComponent(out BuyableComponent buyableComponent)) {
                    CmdBuy(hit.collider.gameObject);
                }
            }
        } else {
            if (!PlayerController.LocalPlayer.currentHoldItem.TryGetComponent(out StorableComponent storableComponent))
                return;
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, G.Instance.storageLayer)) {
                if (!hit.collider.TryGetComponent(out SlotController slotController)) return;
                CmdStoreItem(storableComponent, slotController);
            }
        }
    }

    public void InteractSecondary() {
        if (PlayerController.LocalPlayer.currentHoldItem == null) return;

        NetworkHoldableComponent holdable =
            PlayerController.LocalPlayer.currentHoldItem.GetComponent<NetworkHoldableComponent>();
        Rigidbody rb = holdable.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        holdable.GetComponent<Collider>().isTrigger = false;
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

            NetworkHoldableComponent holdable =
                PlayerController.LocalPlayer.currentHoldItem.GetComponent<NetworkHoldableComponent>();
            Rigidbody rb = holdable.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            holdable.GetComponent<Collider>().isTrigger = false;
            rb.AddForce(totalThrowForce, ForceMode.Impulse);

            CmdThrowItem(totalThrowForce);
        }
    }

    public void Backpack() {
        Ray ray = G.Instance.firstPersonCamera.ScreenPointToRay(Extensions.GetMousePosition());
        if (PlayerController.LocalPlayer.currentHoldItem == null) {
            if (PlayerController.LocalPlayer.backpack == null) {
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance,
                        G.Instance.holdableLayer | G.Instance.storageLayer)) {
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
            playerController.backpack.GetComponent<BackpackController>().Unequip(playerController.obj.position,
                playerController.pickUpPoint.position);
            playerController.backpack = null;
        } else {
            // Надеваем рюкзак
            playerController.backpack = backpackController.gameObject;
            backpackController.Equip();
        }
    }

    [Command(requiresAuthority = false)]
    void CmdStoreItem(StorableComponent storableComponent, SlotController slotController,
        NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        playerController.currentHoldItem.GetComponent<NetworkHoldableComponent>().ServerDrop();
        playerController.currentHoldItem = null;

        storableComponent.netIdentity.RemoveClientAuthority();
        slotController.SetContent(storableComponent);
    }

    [Command(requiresAuthority = false)]
    void CmdBuy(GameObject buyableObject, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        BuyableComponent buyableComponent = buyableObject.GetComponent<BuyableComponent>();
        NetworkHoldableComponent holdableFromBuy = buyableComponent.Buy(senderOrHost);
        // todo потенциально проблемное место, так как нужно переслать sender
        CmdTryPickup(holdableFromBuy, null, sender);
    }

    [Command(requiresAuthority = false)]
    void CmdTryPickup(NetworkHoldableComponent holdable, SlotController slotController,
        NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        if (holdable == null || holdable.isHold) return;

        // Пытаемся взять предмет из Store
        if (slotController != null) {
            slotController.RemoveContent();
        }

        // Пробуем присваивать net identity
        holdable.netIdentity.RemoveClientAuthority();
        holdable.netIdentity.AssignClientAuthority(senderOrHost);
        playerController.currentHoldItem = holdable.gameObject;
        // holdable.GetComponent<NetworkTransformHybrid>().syncDirection = SyncDirection.ClientToServer;
        holdable.ServerPickup(playerController.netIdentity);

        // Проверяем, что предмет не занят другим игроком
        // if (holdable.holder == null) {
        //     playerController.currentHoldItem = holdable.gameObject;
        //     holdable.ServerPickup(playerController.netIdentity);
        // }
    }

    [Command(requiresAuthority = false)]
    void CmdThrowItem(Vector3 totalThrowForce, NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        // Vector3 throwDirection = transform.forward;
        // Vector3 playerVelocity = playerController.characterController.velocity;
        // Vector3 totalThrowForce = (throwDirection * throwForce) + (playerVelocity * 0.5f);

        NetworkHoldableComponent holdable =
            playerController.currentHoldItem.GetComponent<NetworkHoldableComponent>();
        // holdable.GetComponent<NetworkTransformHybrid>().syncDirection = SyncDirection.ServerToClient;
        holdable.ServerThrow(totalThrowForce);
        playerController.currentHoldItem = null;

        // holdable.netIdentity.RemoveClientAuthority();
    }

    [Command(requiresAuthority = false)]
    void CmdDropItem(NetworkConnectionToClient sender = null) {
        NetworkConnectionToClient senderOrHost = sender ?? PlayerController.LocalPlayer.connectionToClient;
        PlayerController playerController = SessionManager.Instance.players[senderOrHost];

        playerController.currentHoldItem.GetComponent<NetworkHoldableComponent>().ServerDrop();
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