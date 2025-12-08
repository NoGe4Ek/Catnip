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
    [SerializeField] public Camera mainCamera;
    [SerializeField] public MovementManager movementManager;
    [SerializeField] public InputManager inputManager;
    [SerializeField] public InteractionManager interactionManager;

    [SerializeField] public LayerMask defaultLayer;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask holdableLayer;
    [SerializeField] public LayerMask storageLayer;
    [SerializeField] public LayerMask storageZoneLayer;
    [SerializeField] public TMP_Text moneyBalanceText;
    public static G Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}
