using System;
using Catnip.Scripts.DI;
using UnityEngine;

namespace Catnip.Scripts.Utils {
public class FloatingText: MonoBehaviour {
    [SerializeField] public Vector3 offset;
    Transform target;

    private void Start() {
        target = transform.parent;
        transform.SetParent(G.Instance.firstPersonCameraCanvas.transform);
        transform.position = Vector3.zero;
    }
    
    private void Update() {
        transform.rotation = Quaternion.LookRotation(transform.position - G.Instance.mainCamera.transform.position);
        transform.position = target.position + offset;
    }
}
}