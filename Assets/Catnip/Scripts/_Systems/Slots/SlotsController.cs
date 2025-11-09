using System.Collections.Generic;
using Catnip.Scripts.Controllers;
using UnityEngine;
namespace Catnip.Scripts._Systems.Slots {
public class SlotsController : MonoBehaviour {
    private GameObject slotWithZeroPivotObject;
    [SerializeField] public GameObject slotObject;
    [SerializeField] public List<GameObject> slots;

    [Header("Rotation Settings")]
    [SerializeField] private float maxXRotation = 10f;

    private void Awake() {
        slotWithZeroPivotObject = gameObject;
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
        Vector3 direction = PlayerController.LocalPlayer.obj.transform.position - slotObject.transform.position;
        float angleX = Mathf.Clamp(direction.y * -10f, -maxXRotation, maxXRotation); // минус для инверсии

        float baseXRotation = -40f;
        Vector3 rotation = slotObject.transform.localEulerAngles;
        rotation.x = baseXRotation + angleX;
        slotObject.transform.localEulerAngles = rotation;
    }
}
}
