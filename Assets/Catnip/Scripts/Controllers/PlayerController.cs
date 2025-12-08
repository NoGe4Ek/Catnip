using System;
using System.Collections.Generic;
using Catnip.Scripts._Systems.Stamina;
using Catnip.Scripts.DI;
using Catnip.Scripts.Managers;
using Catnip.Scripts.Models;
using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Catnip.Scripts.Controllers {
public class PlayerController : NetworkBehaviour {
    [SyncVar(hook = nameof(OnPlayerIdChange))]
    public int playerId;

    public void OnPlayerIdChange(int oldPlayerId, int newPlayerId) {
        if (newPlayerId == -1) return;
        SessionManager.Instance.AddKnownPlayer(newPlayerId);
    }

    [SyncVar(hook = nameof(OnCurrentHoldItemChange))]
    public GameObject currentHoldItem;

    private void OnCurrentHoldItemChange(GameObject oldValue, GameObject newValue) {
        if (newValue == null) {
            leftHand.weight = 0f;
            rightHand.weight = 0f;
        } else {
            leftHand.weight = 1f;
            rightHand.weight = 1f;
        }
    }

    [SyncVar(hook = nameof(OnBackpackChange))]
    public GameObject backpack;

    private void OnBackpackChange(GameObject oldValue, GameObject newValue) {
        backpackMesh.SetActive(newValue != null);
    }

    [SyncVar(hook = nameof(OnIsKnockoutChange))]
    public bool isKnockout;

    private void OnIsKnockoutChange(bool oldValue, bool newValue) {
        if (newValue) {
            Knockout();
        } else {
            Knockin();
        }
    }

    private void Knockout() {
        rbs.ForEach(it => {
            it.linearVelocity = Vector3.zero;
            it.angularVelocity = Vector3.zero;
            it.WakeUp();
        });
        characterController.enabled = false;
        animator.enabled = false;
    }

    private void Knockin() {
        rbs.ForEach(it => {
            it.linearVelocity = Vector3.zero;
            it.angularVelocity = Vector3.zero;
            it.Sleep();
        });
        characterController.enabled = true;
        animator.enabled = true;
    }

    [SerializeField] public GameObject backpackMesh;
    [SerializeField] public Transform pickUpPoint;

    [SerializeField] public GameObject playerMesh;
    [SerializeField] public List<GameObject> fpvPlayerMeshes;

    [SerializeField] public StaminaState staminaState;

    [SerializeField] public List<Rigidbody> rbs;
    [SerializeField] public Rig leftHand;
    [SerializeField] public Transform leftHandTarget;
    [SerializeField] public Rig rightHand;
    [SerializeField] public Transform rightHandTarget;
    [SerializeField] public Rig head;
    [SerializeField] public Transform headTarget;

    public Transform orientation;

    public Transform obj;

    // public Transform player;
    public float movementSpeed = 3.0f, rotationSpeed = 5.0f, jumpForce = 10.0f, gravity = -30f;
    public GameObject firstPersonFollow, thirdPersonFollow;
    public CinemachineCamera firstPersonFollowCamera, thirdPersonFollowCamera; // todo remove after test
    public CharacterController characterController;
    public PersonView personView = PersonView.First;
    public Animator animator;

    [SyncVar(hook = nameof(OnHolderChanged))]
    public int balance;

    private void OnHolderChanged(int oldValue, int newValue) {
        G.Instance.moneyBalanceText.text = newValue.ToString();
    }

    public static PlayerController LocalPlayer;

    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();
        // G.Instance.thirdPersonCamera.gameObject.SetActive(false);
        firstPersonFollow.SetActive(true);
        fpvPlayerMeshes.ForEach(it => it.SetActive(true));
        thirdPersonFollow.SetActive(false);
        playerMesh.SetActive(false);

        gameObject.SetActive(true);
        G.Instance.movementManager.playerController = this;
        G.Instance.movementManager.gameObject.SetActive(true);
        G.Instance.inputManager.gameObject.SetActive(true);

        leftHand.weight = 0f;
        rightHand.weight = 0f;

        LocalPlayer = this;
        EventManager.TriggerEvent(EventKey.LocalPlayerReady, LocalPlayer);
    }

    public void Awake() {
        // Должно быть проинициализированно перед изменением в хуке, чтобы первое первое изменение применилось
        playerId = -1;
    }
}
}