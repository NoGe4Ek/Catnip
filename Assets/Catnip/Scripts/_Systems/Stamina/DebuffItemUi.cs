using System;
using UnityEngine;
using UnityEngine.UI;

namespace Catnip.Scripts._Systems.Stamina {
public class DebuffItemUi : MonoBehaviour {
    [SerializeField] public RectTransform rect;
    [SerializeField] public GameObject iconPrefab;

    private GameObject iconInstance;
    private RectTransform iconRect;

    public void Start() {
        iconInstance = Instantiate(iconPrefab, transform.parent.parent);
        iconRect = iconInstance.GetComponent<RectTransform>();
    }

    public void FixedUpdate() {
        Vector3 center = rect.TransformPoint(rect.rect.center);

        float newX = center.x;
        float newY = rect.position.y + 20f;
        iconInstance.transform.position = new Vector3(newX, newY, 0f);
    }
}
}