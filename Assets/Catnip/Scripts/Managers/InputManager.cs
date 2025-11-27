using Catnip.Scripts._Systems.Stamina;
using Catnip.Scripts.DI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Catnip.Scripts.Managers {
public class InputManager : MonoBehaviour {
    public InputAction moveAction,
        lookAction,
        jumpAction,
        sprintAction,
        interactPrimaryAction,
        interactSecondaryAction,
        interactTertiaryAction;

    private Vector2 moveVector, lookVector;
    private bool isPause;

    public void Start() {
        moveAction = InputSystem.actions.FindAction("Move");
        moveAction.performed += ctx => moveVector = ctx.ReadValue<Vector2>();
        moveAction.canceled += _ => moveVector = Vector2.zero;

        sprintAction = InputSystem.actions.FindAction("Sprint");
        sprintAction.performed += _ => {
            Debug.Log("Sprint pressed");
            G.Instance.movementManager.SetRunning(true);
            StaminaManager.Instance.StartSprint();
        };
        sprintAction.canceled += _ => {
            Debug.Log("Sprint released");
            G.Instance.movementManager.SetRunning(false);
            StaminaManager.Instance.EndSprint();
        };

        lookAction = InputSystem.actions.FindAction("Look");
        lookAction.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        lookAction.canceled += _ => lookVector = Vector2.zero;

        jumpAction = InputSystem.actions.FindAction("Jump");
        jumpAction.performed += _ => {
            G.Instance.movementManager.Jump();
            StaminaManager.Instance.StartJump();
        };

        jumpAction = InputSystem.actions.FindAction("PersonView");
        jumpAction.performed += _ => G.Instance.movementManager.NextPersonView();

        interactPrimaryAction = InputSystem.actions.FindAction("Attack");
        interactPrimaryAction.started += _ => G.Instance.interactionManager.InteractPrimary();

        interactSecondaryAction = InputSystem.actions.FindAction("InteractSecondary");
        interactSecondaryAction.started += _ => G.Instance.interactionManager.InteractSecondary();

        interactTertiaryAction = InputSystem.actions.FindAction("InteractTertiary");
        interactTertiaryAction.started += _ => G.Instance.interactionManager.InteractTertiary();

        interactTertiaryAction = InputSystem.actions.FindAction("Throw");
        interactTertiaryAction.started += _ => G.Instance.interactionManager.Throw();

        interactTertiaryAction = InputSystem.actions.FindAction("Backpack");
        interactTertiaryAction.started += _ => G.Instance.interactionManager.Backpack();

        interactTertiaryAction = InputSystem.actions.FindAction("Spawn");
        interactTertiaryAction.started += _ => G.Instance.interactionManager.Spawn();

        interactTertiaryAction = InputSystem.actions.FindAction("Test");
        interactTertiaryAction.started += _ => G.Instance.interactionManager.Test();

        interactTertiaryAction = InputSystem.actions.FindAction("Pause");
        interactTertiaryAction.started += _ => {
            isPause = !isPause;
            G.Instance.firstPersonCamera.GetComponent<CinemachineBrain>().enabled = !isPause;
        };

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update() {
        if (G.Instance.movementManager.playerController == null) return;
        if (!isPause) G.Instance.movementManager.Move(moveVector);
        if (!isPause) G.Instance.movementManager.Rotate(lookVector);
    }
}
}