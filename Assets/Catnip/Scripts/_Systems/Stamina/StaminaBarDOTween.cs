// using System.Collections.Generic;
// using DG.Tweening;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace Catnip.Scripts._Systems.Stamina {
// public class StaminaBarDOTween : MonoBehaviour {
//     [Header("Main References")] [SerializeField]
//     private RectTransform staminaBarRect;
//
//     [SerializeField] private Image staminaFill;
//     [SerializeField] private RectTransform debuffsContainer;
//     [SerializeField] private GameObject debuffSegmentPrefab;
//
//     [Header("Colors")] [SerializeField] private Color staminaColor = Color.green;
//
//     [SerializeField] private List<Color> debuffColors = new List<Color> {
//         new Color(1f, 0.3f, 0.3f), // Красный
//         new Color(1f, 0.8f, 0.3f), // Желтый
//         new Color(0.8f, 0.3f, 1f), // Фиолетовый
//         new Color(0.3f, 0.8f, 1f) // Голубой
//     };
//
//     [Header("Animation Settings")] [SerializeField]
//     private float staminaChangeDuration = 0.5f;
//
//     [SerializeField] private Ease staminaEase = Ease.OutCubic;
//     [SerializeField] private float debuffAppearDuration = 0.3f;
//
//     private StaminaState staminaState;
//     private List<Image> activeDebuffSegments = new List<Image>();
//     private float currentStaminaPercent = 1f;
//     private Tween currentStaminaTween;
//
//     public void Start() {
//         staminaState = new StaminaState(100, 70, new List<int> { 20, 10 });
//         Initialize(staminaState);
//     }
//
//     public void Update() {
//         if (Input.GetKeyDown(KeyCode.Space)) {
//             staminaState.currentValue -= 10;
//             UpdateStaminaBar(); // С анимацией
//         }
//
//         if (Input.GetKeyDown(KeyCode.D)) {
//             AddDebuffWithAnimation(15); // Добавить дебафф
//         }
//     }
//
//     public void Initialize(StaminaState state) {
//         staminaState = state;
//         currentStaminaPercent = staminaState.GetCurrentValueCoerce();
//         UpdateVisualsImmediate();
//     }
//
//     public void UpdateStaminaBar(bool animate = true) {
//         float targetPercent = staminaState.GetCurrentValueCoerce();
//
//         if (animate) {
//             // Анимируем изменение стамины
//             currentStaminaTween?.Kill();
//             currentStaminaTween = DOTween.To(
//                 () => currentStaminaPercent,
//                 x => {
//                     currentStaminaPercent = x;
//                     UpdateStaminaFill();
//                     UpdateDebuffsSegments();
//                     UpdateDebuffsPosition();
//                 },
//                 targetPercent,
//                 staminaChangeDuration
//             ).SetEase(staminaEase);
//         } else {
//             currentStaminaPercent = targetPercent;
//             UpdateVisualsImmediate();
//         }
//     }
//
//     private void UpdateVisualsImmediate() {
//         UpdateStaminaFill();
//         UpdateDebuffsSegments();
//     }
//
//     private void UpdateStaminaFill() {
//         // Обновляем ширину заполнителя стамины
//         staminaFill.rectTransform.anchorMax = new Vector2(currentStaminaPercent, 1f);
//     }
//
//     private void UpdateDebuffsSegments() {
//         // Удаляем старые дебаффы
//         ClearDebuffSegments();
//
//         var debuffValues = staminaState.GetDebuffValuesCoerce();
//         float currentPos = currentStaminaPercent;
//
//         // Создаем новые дебафф сегменты
//         for (int i = 0; i < debuffValues.Count; i++) {
//             CreateDebuffSegment(currentPos, debuffValues[i], i);
//             currentPos += debuffValues[i];
//         }
//     }
//
//     private void UpdateDebuffsPosition() {
//         // Обновляем позиции существующих дебаффов при анимации стамины
//         var debuffValues = staminaState.GetDebuffValuesCoerce();
//         float currentPos = currentStaminaPercent;
//
//         for (int i = 0; i < debuffValues.Count && i < activeDebuffSegments.Count; i++) {
//             var segment = activeDebuffSegments[i];
//             var rectTransform = segment.rectTransform;
//
//             rectTransform.anchorMin = new Vector2(currentPos, 0f);
//             rectTransform.anchorMax = new Vector2(currentPos + debuffValues[i], 1f);
//
//             currentPos += debuffValues[i];
//         }
//     }
//
//     private void CreateDebuffSegment(float startPosition, float debuffValue, int index) {
//         if (debuffSegmentPrefab == null) return;
//
//         var debuffObject = Instantiate(debuffSegmentPrefab, debuffsContainer);
//         var debuffImage = debuffObject.GetComponent<Image>();
//
//         // Устанавливаем цвет
//         Color debuffColor = debuffColors[index % debuffColors.Count];
//         debuffImage.color = debuffColor;
//
//         // Настраиваем позицию и размер
//         var rectTransform = debuffObject.GetComponent<RectTransform>();
//         rectTransform.anchorMin = new Vector2(startPosition, 0f);
//         rectTransform.anchorMax = new Vector2(startPosition + debuffValue, 1f);
//         rectTransform.offsetMin = Vector2.zero;
//         rectTransform.offsetMax = Vector2.zero;
//
//         // Анимация появления
//         debuffImage.DOFade(0f, 0f);
//         debuffImage.DOFade(1f, debuffAppearDuration);
//
//         activeDebuffSegments.Add(debuffImage);
//     }
//
//     private void ClearDebuffSegments() {
//         foreach (var segment in activeDebuffSegments) {
//             if (segment != null) {
//                 // Анимация исчезновения
//                 segment.DOFade(0f, debuffAppearDuration)
//                     .OnComplete(() => Destroy(segment.gameObject));
//             }
//         }
//
//         activeDebuffSegments.Clear();
//     }
//
//     // Метод для добавления дебаффа с анимацией
//     public void AddDebuffWithAnimation(int debuffValue) {
//         staminaState.debuffValues.Add(debuffValue);
//
//         // Небольшая задержка перед обновлением для драматизма
//         DOVirtual.DelayedCall(0.2f, () => UpdateStaminaBar());
//     }
//
//     // Метод для удаления дебаффа
//     public void RemoveDebuff(int index, bool animate = true) {
//         if (index >= 0 && index < staminaState.debuffValues.Count) {
//             staminaState.debuffValues.RemoveAt(index);
//             UpdateStaminaBar(animate);
//         }
//     }
//
//     void OnDestroy() {
//         currentStaminaTween?.Kill();
//     }
// }
// }