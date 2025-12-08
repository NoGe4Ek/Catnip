using System;
using System.Collections.Generic;
using System.Linq;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Catnip.Scripts.Utils;
using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Slots {
public class SlotsController : NetworkBehaviour {
    [SerializeField] public GameObject backgroundPrefab;
    [SerializeField] public GameObject slotPrefab;
    [SerializeField] public float slotStartOffset = -0.04766257f;
    [SerializeField] public float slotCommonOffset = -0.09532513f;
    [SerializeField] public float slotZOffset = 0.01f;
    [SerializeField] public bool showWithEmptyHands;

    [SyncVar(hook = nameof(OnBackgroundObjectChange))]
    public GameObject backgroundObject;

    private void OnBackgroundObjectChange(GameObject oldValue, GameObject newValue) {
        if (newValue == null) return;
        // Нельзя объединить с инстанциированием, иначе сервер не сможет заспавнить этот объект внутри родителя с NetworkIdentity.
        // А вот изменить ему родителя и позицию после спавна уже можно
        newValue.transform.SetParent(transform);
        newValue.transform.localPosition = Vector3.zero;
        newValue.transform.localRotation = Quaternion.identity;
        newValue.layer = G.Instance.storageZoneLayer.ToLayer();
        newValue.transform.localScale = new Vector3(slotsSettings.width, slotsSettings.height, 1);
        Vector3 renderCenter = newValue.GetLocalRenderCenter();
        newValue.transform.localPosition = new Vector3(-renderCenter.x * slotsSettings.width, 0, 0);
    }

    public readonly List<SlotController> slots = new();

    [SyncVar] public SlotsSettings slotsSettings;

    public bool CanShow() =>
        showWithEmptyHands || !showWithEmptyHands && PlayerController.LocalPlayer.currentHoldItem != null;

    public bool IsSlotsEmpty() {
        return slots.All(slot => slot.storeObject == null);
    }

    [Header("Rotation Settings")] [SerializeField]
    private float maxXRotation = 10f;

    public void Awake() {
        ISlotsOwner slotsOwner = gameObject.FindComponentInParentRecursive<ISlotsOwner>();
        if (slotsOwner == null) throw new MissingComponentException();
        slotsSettings = slotsOwner.GetSlotsSettings();
    }

    private void Start() {
        if (isHost || isServer) {
            GameObject backgroundInstance = Instantiate(backgroundPrefab, transform);
            NetworkServer.Spawn(backgroundInstance);
            backgroundObject = backgroundInstance;

            for (int w = 0; w < slotsSettings.width; w++) {
                for (int h = 0; h < slotsSettings.height; h++) {
                    GameObject slotInstance = Instantiate(slotPrefab, transform);
                    NetworkServer.Spawn(slotInstance);

                    SlotController slotController = slotInstance.GetComponent<SlotController>();
                    slotController.slotsController = this;
                    slotController.slotPosition = new SlotPosition(w, h);
                    slots.Add(slotController);
                }
            }
        }
        gameObject.SetActive(false);
    }

    private void Update() {
        UpdateRotation();
    }

    private void UpdateRotation() {
        if (PlayerController.LocalPlayer == null) return;

        // Поворот основного объекта по Y
        Vector3 targetPosition = PlayerController.LocalPlayer.obj.transform.position;
        targetPosition.y = transform.position.y;
        transform.LookAt(targetPosition);

        // Поворот slotObject по X с сохранением базового угла
        Vector3 direction = PlayerController.LocalPlayer.obj.transform.position - transform.position;
        float angleX = Mathf.Clamp(direction.y * -10f, -maxXRotation, maxXRotation); // минус для инверсии

        float baseXRotation = -40f;
        Vector3 rotation = transform.localEulerAngles;
        rotation.x = baseXRotation + angleX;
        transform.localEulerAngles = rotation;
    }
}
}