using UnityEngine;
public class RespawnController : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        // Transform targetToRespawn = other.transform.parent != null ? other.transform.parent : other.transform;
        Transform targetToRespawn = other.transform;

        CharacterController controller = targetToRespawn.GetComponent<CharacterController>();
        if (controller != null) {
            // Временно отключаем контроллер, перемещаем, включаем обратно
            controller.enabled = false;
        }
        // Телепортируем объект в точку (0, 0, 0)
        targetToRespawn.position = new Vector3(0, 2, 0);

        if (controller != null) {
            controller.enabled = true;
        }


        // Если у объекта есть Rigidbody, сбрасываем его velocity
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Debug.Log($"Объект {other.name} телепортирован в (0, 0, 0)");
    }

    private void OnCollisionEnter(Collision collision) {
        // Телепортируем объект в точку (0, 0, 0)
        collision.transform.position = Vector3.zero;

        // Если у объекта есть Rigidbody, сбрасываем его velocity
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Debug.Log($"Объект {collision.gameObject.name} телепортирован в (0, 0, 0)");
    }
}
