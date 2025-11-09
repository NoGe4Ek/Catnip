using UnityEngine;
using UnityEngine.AI;

namespace Catnip.Scripts.Controllers {
public class NpcController : MonoBehaviour {
    [SerializeField] public NavMeshAgent navMeshAgent;
    [SerializeField] public Animator animator;

    public void MoveTo(Vector3 destination) {
        navMeshAgent.SetDestination(destination);
    }

    private void UpdateAnimation(Vector3 movementVector) {
        Vector3 normalizedMovement = navMeshAgent.desiredVelocity.normalized;

        Vector3 forwardVector = Vector3.Project(normalizedMovement, transform.forward);
        Vector3 rightVector = Vector3.Project(normalizedMovement, transform.right);

        float forwardVelocity = forwardVector.magnitude * Vector3.Dot(forwardVector, transform.forward);
        float rightVelocity = rightVector.magnitude * Vector3.Dot(rightVector, transform.right);
    }
}
}