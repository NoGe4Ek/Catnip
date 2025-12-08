using System;
using System.Collections;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.DI;
using Catnip.Scripts.Models;
using Catnip.Scripts.Utils;
using Unity.Cinemachine;
using UnityEngine;

namespace Catnip.Scripts.Managers {
public class MovementManager : MonoBehaviour {
    public PlayerController playerController;
    float rotationY;
    float verticalVelocity;

    // Добавляем переменные для анимации
    public float walkingSpeed = 2f;
    public float runningSpeed = 6f;
    public float currentSpeed;
    private float animationInterpolation = 1f;
    private bool isRunning = false;

    void Update() {
        playerController.orientation.position = playerController.obj.position;
    }

    public void Move(Vector3 movementVector) {
        UpdateAnimation(movementVector);

        switch (playerController.personView) {
            case PersonView.Third: {
                Vector3 viewDir = playerController.obj.position - new Vector3(
                    G.Instance.mainCamera.transform.position.x, playerController.obj.position.y,
                    G.Instance.mainCamera.transform.position.z);
                playerController.orientation.forward = viewDir.normalized;

                // rotate player object based on movement input
                float horizontalInput = movementVector.x;
                float verticalInput = movementVector.y;
                Vector3 inputDir = playerController.orientation.forward * verticalInput +
                                   playerController.orientation.right * horizontalInput;

                if (inputDir != Vector3.zero)
                    playerController.obj.forward = Vector3.Slerp(playerController.obj.forward, inputDir.normalized,
                        Time.deltaTime * playerController.rotationSpeed);

                Vector3 move = inputDir * playerController.movementSpeed * Time.deltaTime;
                playerController.characterController.Move(move);
                break;
            }
            case PersonView.First: {
                playerController.orientation.forward = playerController.obj.forward; // Направление, куда смотрит игрок
                
                // rotate player object based on movement input
                float horizontalInput = movementVector.x;
                float verticalInput = movementVector.y;
                Vector3 inputDir = playerController.orientation.forward * verticalInput +
                                   playerController.orientation.right * horizontalInput;
                
                Vector3 move = inputDir * playerController.movementSpeed * Time.deltaTime;
                playerController.characterController.Move(move);
                break;
            }
        }

        // Debug.Log("Is grounded: " + IsGrounded());
        // handle jump
        // !playerController.characterController.isGrounded
        // if (!IsGrounded()) {
        //     verticalVelocity += playerController.gravity * Time.deltaTime;
        //     playerController.characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
        // } else {
        //     playerController.characterController.Move(new Vector3(0, playerController.gravity, 0) * Time.deltaTime);
        // }
        // Debug.Log("verticalVelocity: " + verticalVelocity);
        if (verticalVelocity <= 0 && IsGrounded())
            verticalVelocity = 0;
        else
            verticalVelocity += playerController.gravity * Time.deltaTime;

        playerController.characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    public bool IsGrounded() {
        float sphereRadius = 0.3f; // Радиус сферы
        float sphereLength = 0.2f; // Длина проверки

        // Используем transform из playerController.obj
        Transform characterTransform = playerController.orientation.transform;
        Vector3 sphereOrigin = characterTransform.position + Vector3.up * sphereRadius;

        // Визуализация для отладки
        Extensions.DrawSphereCastDebug(sphereOrigin, Vector3.down, sphereRadius, sphereLength,
            Color.green);

        if (Physics.SphereCast(sphereOrigin, sphereRadius, Vector3.down, out RaycastHit hit, sphereLength,
                G.Instance.groundLayer)) {
            // Если попали - рисуем красным (земля найдена)
            Extensions.DrawSphereCastDebug(sphereOrigin, Vector3.down, sphereRadius, sphereLength,
                Color.red);
            Debug.DrawLine(sphereOrigin, hit.point, Color.red, 2f);

            return true;
        }

        return false;
    }

    public void Rotate(Vector3 rotationVector) {
        if (playerController.personView == PersonView.First) {
            // Получаем угол поворота камеры по оси Y
            rotationY = G.Instance.mainCamera.transform.eulerAngles.y;
            playerController.obj.rotation = Quaternion.Euler(0, rotationY, 0);
        }
        // if (personView == PersonView.First) {
        //     rotationY += rotationVector.x * rotationSpeed * Time.deltaTime;
        //     obj.localRotation = Quaternion.Euler(0, rotationY, 0);
        // }
    }

    public void Jump() {
        if (IsGrounded()) {
            verticalVelocity = playerController.jumpForce;
            playerController.animator.SetTrigger("Jump");
        }
    }

    public void SetRunning(bool running) {
        if (running) {
            playerController.movementSpeed = 6f;
        } else {
            playerController.movementSpeed = 3f;
        }

        isRunning = running;
    }

    private void UpdateAnimation(Vector3 movementVector) {
        if (playerController.animator == null) return;

        // Определяем, бежит ли персонаж на основе movementVector и флага бега
        bool hasSignificantMovement = movementVector.magnitude > 0.1f;
        bool isMovingForward = movementVector.y > 0.1f;

        if (isRunning && hasSignificantMovement && isMovingForward) {
            // Бег вперед
            animationInterpolation = Mathf.Lerp(animationInterpolation, 1.5f, Time.deltaTime * 3);
            currentSpeed = Mathf.Lerp(currentSpeed, runningSpeed, Time.deltaTime * 3);
        } else {
            // Ходьба или движение назад/в сторону
            animationInterpolation = Mathf.Lerp(animationInterpolation, 1f, Time.deltaTime * 3);
            currentSpeed = Mathf.Lerp(currentSpeed, walkingSpeed, Time.deltaTime * 3);
        }

        // Устанавливаем параметры анимации напрямую из movementVector
        playerController.animator.SetFloat("x", movementVector.x * animationInterpolation);
        playerController.animator.SetFloat("y", movementVector.y * animationInterpolation);

        // Расчет magnitude для анимации
        float magnitude = movementVector.magnitude;
        playerController.animator.SetFloat("magnitude", magnitude * animationInterpolation);

        // Анимация приземления/падения
        // playerController.animator.SetBool("isGrounded", playerController.characterController.isGrounded);
        // playerController.animator.SetFloat("verticalVelocity", verticalVelocity);
    }

    public void NextPersonView() {
        switch (playerController.personView) {
            case PersonView.First:
                SetThirdPersonView();
                break;
            case PersonView.Third:
                SetFirstPersonView();
                break;
        }
    }

    public void SetFirstPersonView() {
        // G.Instance.thirdPersonCamera.gameObject.SetActive(false);
        playerController.thirdPersonFollow.gameObject.SetActive(false);
        // playerController.thirdPersonFollowCamera.enabled = false;
        // G.Instance.firstPersonCamera.gameObject.SetActive(true);
        
        // Обновить firstPersonFollow rotation, так как он был отключен и не знает актуальное состояние поворота игрока.
        // Позволяет переключить камеру на 1st view в сторону, которую персонаж СМОТРЕЛ в 3d view
        float value = Mathf.DeltaAngle(0, playerController.obj.rotation.eulerAngles.y);
        playerController.firstPersonFollow.GetComponent<CinemachinePanTilt>().PanAxis.Value = value;
        playerController.firstPersonFollow.transform.rotation = playerController.obj.rotation;
        
        playerController.firstPersonFollow.gameObject.SetActive(true);
        // playerController.firstPersonFollowCamera.enabled = true;

        new Task(WaitBlendAndChangeViewTask());

        // new Task(SmoothTransitionToFirstPerson());
    }

    private IEnumerator WaitBlendAndChangeViewTask() {
        while (G.Instance.mainCamera.GetComponent<CinemachineBrain>().IsBlending) {
            yield return null;
        }
        playerController.fpvPlayerMeshes.ForEach(it => it.SetActive(true));
        playerController.playerMesh.SetActive(false);
        playerController.personView = PersonView.First;
    }

    // private IEnumerator SmoothTransitionToFirstPerson() {
    //     // Получаем позицию и вращение третьего лица
    //     Vector3 startPos = G.Instance.thirdPersonCamera.transform.position;
    //     Quaternion startRot = G.Instance.thirdPersonCamera.transform.rotation;
    //
    //     // Получаем позицию и вращение первого лица
    //     Vector3 endPos = G.Instance.firstPersonCamera.transform.position;
    //     Quaternion endRot = G.Instance.firstPersonCamera.transform.rotation;
    //
    //     // Активируем камеру первого лица в начальной позиции
    //     G.Instance.firstPersonCamera.GetComponent<CinemachineBrain>().enabled = false;
    //     G.Instance.firstPersonCamera.gameObject.SetActive(true);
    //     G.Instance.firstPersonCamera.transform.position = startPos;
    //     G.Instance.firstPersonCamera.transform.rotation = startRot;
    //
    //     // Деактивируем камеру третьего лица
    //     G.Instance.thirdPersonCamera.gameObject.SetActive(false);
    //
    //     // Настраиваем Follow компоненты
    //     playerController.thirdPersonFollow.gameObject.SetActive(false);
    //     playerController.firstPersonFollow.gameObject.SetActive(true);
    //
    //     // Переключаем меши
    //     playerController.fpvPlayerMeshes.ForEach(it => it.SetActive(true));
    //     playerController.playerMesh.SetActive(false);
    //     playerController.personView = PersonView.First;
    //
    //     // Плавный переход
    //     float duration = 0.5f; // Длительность перехода
    //     float elapsed = 0f;
    //
    //     while (elapsed < duration) {
    //         elapsed += Time.deltaTime;
    //         float t = elapsed / duration;
    //
    //         // Можно использовать разные кривые для плавности
    //         t = Mathf.SmoothStep(0, 1, t);
    //
    //         // Интерполяция позиции и вращения
    //         G.Instance.firstPersonCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
    //         G.Instance.firstPersonCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
    //
    //         yield return null;
    //     }
    //
    //     // Финализируем позицию
    //     G.Instance.firstPersonCamera.transform.position = endPos;
    //     G.Instance.firstPersonCamera.transform.rotation = endRot;
    //     G.Instance.firstPersonCamera.GetComponent<CinemachineBrain>().enabled = true;
    // }

    public void SetThirdPersonView() {
        // G.Instance.firstPersonCamera.gameObject.SetActive(false);
        playerController.firstPersonFollow.gameObject.SetActive(false);
        // G.Instance.thirdPersonCamera.gameObject.SetActive(true);
        playerController.thirdPersonFollow.gameObject.SetActive(true);

        playerController.fpvPlayerMeshes.ForEach(it => it.SetActive(false));
        playerController.playerMesh.SetActive(true);
        playerController.personView = PersonView.Third;

        // new Task(SmoothTransitionToThirdPerson());
    }

    // private IEnumerator SmoothTransitionToThirdPerson() {
    //     // Получаем позицию и вращение первого лица
    //     Vector3 startPos = G.Instance.firstPersonCamera.transform.position;
    //     Quaternion startRot = G.Instance.firstPersonCamera.transform.rotation;
    //
    //     // Получаем позицию и вращение третьего лица
    //     Vector3 endPos = G.Instance.thirdPersonCamera.transform.position;
    //     Quaternion endRot = G.Instance.thirdPersonCamera.transform.rotation;
    //
    //     // Активируем камеру третьего лица в начальной позиции
    //     G.Instance.thirdPersonCamera.GetComponent<CinemachineBrain>().enabled = false;
    //     G.Instance.thirdPersonCamera.gameObject.SetActive(true);
    //     G.Instance.thirdPersonCamera.transform.position = startPos;
    //     G.Instance.thirdPersonCamera.transform.rotation = startRot;
    //
    //     // Деактивируем камеру первого лица
    //     G.Instance.firstPersonCamera.gameObject.SetActive(false);
    //
    //     // Настраиваем Follow компоненты
    //     playerController.firstPersonFollow.gameObject.SetActive(false);
    //     playerController.thirdPersonFollow.gameObject.SetActive(true);
    //
    //     // Переключаем меши
    //     playerController.fpvPlayerMeshes.ForEach(it => it.SetActive(false));
    //     playerController.playerMesh.SetActive(true);
    //     playerController.personView = PersonView.Third;
    //
    //     // Плавный переход
    //     float duration = 0.5f; // Длительность перехода
    //     float elapsed = 0f;
    //
    //     while (elapsed < duration) {
    //         elapsed += Time.deltaTime;
    //         float t = elapsed / duration;
    //
    //         // Можно использовать разные кривые для плавности
    //         t = Mathf.SmoothStep(0, 1, t);
    //
    //         // Интерполяция позиции и вращения
    //         G.Instance.thirdPersonCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
    //         G.Instance.thirdPersonCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
    //
    //         yield return null;
    //     }
    //
    //     // Финализируем позицию
    //     G.Instance.thirdPersonCamera.transform.position = endPos;
    //     G.Instance.thirdPersonCamera.transform.rotation = endRot;
    //     G.Instance.thirdPersonCamera.GetComponent<CinemachineBrain>().enabled = true;
    // }
}
}