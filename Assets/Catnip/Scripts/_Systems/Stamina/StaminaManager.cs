using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Catnip.Scripts._Systems.Stamina {
public class StaminaManager : MonoBehaviour {
    [SerializeField] public Transform debuffParent;
    [SerializeField] public GameObject debuffItemPrefab;
    [SerializeField] public GameObject staminaItem;
    [SerializeField] public Image staminaSliderImage;


    private float lastActivityTime;
    private Task sprintTask;
    private Task restTask;

    public void AddDebuff(DebuffType type) {
        GameObject debuffItem = Instantiate(debuffItemPrefab, debuffParent);
        DebuffItemUi debuffItemUi = debuffItem.GetComponent<DebuffItemUi>();
        PlayerController.LocalPlayer.staminaState.AddDebuff(type, debuffItemUi);

        new Task(StartDebuffTask(type));
    }

    private IEnumerator StartDebuffTask(DebuffType type) {
        while (PlayerController.LocalPlayer.staminaState.GetCurrentMaxValueWithDebuff() > 0) {
            yield return new WaitForSeconds(0.5f);
            PlayerController.LocalPlayer.staminaState.UpdateDebuff(type, 1);
        }
    }

    public void StartJump() {
        PlayerController.LocalPlayer.staminaState.UpdateCurrentValue(-10);
        lastActivityTime = Time.time;
        restTask?.Stop();
    }

    public void StartSprint() {
        sprintTask = new Task(StartSprintTask());
    }

    public void EndSprint() {
        sprintTask.Stop();
    }

    private IEnumerator StartSprintTask() {
        while (PlayerController.LocalPlayer.staminaState.GetCurrentValue() > 0) {
            yield return new WaitForSeconds(0.1f);
            PlayerController.LocalPlayer.staminaState.UpdateCurrentValue(-1);
            lastActivityTime = Time.time;
            restTask?.Stop();
        }
    }

    private void Start() {
        EventManager.AddListener<PlayerController>(EventKey.LocalPlayerReady, controller => {
            PlayerController.LocalPlayer.staminaState = new(100, 100, new List<int>());
            AddDebuff(DebuffType.SomeDebuff);
            AddDebuff(DebuffType.SomeDebuff1);
            AddDebuff(DebuffType.SomeDebuff2);
        });
    }

    private void FixedUpdate() {
        if (PlayerController.LocalPlayer == null) return;
        // Stamina width
        RectTransform rt = staminaItem.transform as RectTransform;
        if (rt == null) return;
        float newWidth = StaminaState.MaxStaminaWidth - PlayerController.LocalPlayer.staminaState.GetDebuffsWidth();
        rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
        
        // Debuffs width
        foreach (var staminaStateDebuffValue in PlayerController.LocalPlayer.staminaState.GetDebuffsIterator()) {
            var value = staminaStateDebuffValue.Value.Item1;
            float newWidthForDebuff = value * StaminaState.ValueToWidthCoeff;
            
            var transform = staminaStateDebuffValue.Value.Item2.rect;
            transform.sizeDelta = new Vector2(newWidthForDebuff, transform.sizeDelta.y);
        }
    }

    private void Update() {
        if (PlayerController.LocalPlayer == null) return;
        staminaSliderImage.fillAmount = PlayerController.LocalPlayer.staminaState.GetCurrentValueCoerce();

        // Rest logic
        if (Time.time - lastActivityTime >= 3 && restTask is not { Running: true }) {
            restTask = new Task(StartRestTask());
        }
    }

    private IEnumerator StartRestTask() {
        while (PlayerController.LocalPlayer.staminaState.GetCurrentValue() <
               PlayerController.LocalPlayer.staminaState.GetCurrentMaxValueWithDebuff()) {
            yield return new WaitForSeconds(0.6f);
            PlayerController.LocalPlayer.staminaState.UpdateCurrentValue(+1);
            lastActivityTime = Time.time;
        }
    }

    public static StaminaManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }
}
}