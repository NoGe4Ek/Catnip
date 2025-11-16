using System;
using System.Collections.Generic;
using System.Linq;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.DI;
using Catnip.Scripts.Utils;
using UnityEngine;

namespace Catnip.Scripts._Systems.Slots {
public class SlotsController : MonoBehaviour {
    [SerializeField] public GameObject backgroundPrefab;
    [SerializeField] public GameObject slotPrefab;
    [SerializeField] public float slotStartOffset = -0.04766257f;
    [SerializeField] public float slotCommonOffset = -0.09532513f;
    [SerializeField] public float slotZOffset = 0.01f;

    private SlotsSettings slotsSettings;
    private readonly List<SlotController> slots = new();

    public bool IsSlotsEmpty() {
        return slots.All(slot => slot.storeObject == null);
    }

    [Header("Rotation Settings")] [SerializeField]
    private float maxXRotation = 10f;

    public void Awake() {
        ISlotsOwner slotsOwner = gameObject.FindComponentInParentRecursive<ISlotsOwner>();
        if (slotsOwner == null) throw new MissingComponentException();
        slotsSettings = slotsOwner.GetSlotsSettings();


        gameObject.SetActive(false);
    }

    public void Start() {
        GameObject backgroundInstance = Instantiate(backgroundPrefab, transform);
        backgroundInstance.transform.localScale = new Vector3(slotsSettings.width, slotsSettings.height, 1);
        Vector3 renderCenter = backgroundInstance.GetLocalRenderCenter();
        backgroundInstance.transform.localPosition = new Vector3(-renderCenter.x * slotsSettings.width, 0, 0);

        for (int w = 0; w < slotsSettings.width; w++) {
            for (int h = 0; h < slotsSettings.height; h++) {
                GameObject slotInstance = Instantiate(slotPrefab, transform);
                slotInstance.layer = G.Instance.storageLayer.ToLayer();
                slotInstance.transform.localPosition =
                    new Vector3(
                        slotStartOffset + w * slotCommonOffset + (-renderCenter.x * slotsSettings.width),
                        -slotStartOffset + h * -slotCommonOffset,
                        slotZOffset
                    );

                SlotController slotController = slotInstance.GetComponent<SlotController>();
                slotController.slotPosition = new SlotPosition(w, h);
                slots.Add(slotController);
            }
        }
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