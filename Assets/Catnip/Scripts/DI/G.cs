using Catnip.Scripts.Controllers;
using Catnip.Scripts.Managers;
using TMPro;
using UnityEngine;
namespace Catnip.Scripts.DI {
public class G : MonoBehaviour {
    [Header("Debug settings")]
    [SerializeField] public bool useLogging;
    
    [Header("Other")]
    [SerializeField] public Transform tipsContainer;
    [SerializeField] public GameObject tipPrefab;
    [SerializeField] public Canvas firstPersonCameraCanvas;
    [SerializeField] public Camera firstPersonCamera;
    [SerializeField] public Camera thirdPersonCamera;
    [SerializeField] public MovementManager movementManager;
    [SerializeField] public InputManager inputManager;
    [SerializeField] public InteractionManager interactionManager;

    [SerializeField] public LayerMask interactableLayer;
    [SerializeField] public TMP_Text moneyBalanceText;
    public static G Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}
