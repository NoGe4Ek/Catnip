using Catnip.Scripts.DI;
using UnityEngine;

namespace Catnip.Scripts.Utils {
public class LookAtPlayer : MonoBehaviour {
    private void LateUpdate() {
        //transform.rotation = Quaternion.identity;
        transform.LookAt(transform.position + G.Instance.firstPersonCamera.transform.forward);
    }
}
}