using System;
using Catnip.Scripts.Controllers;
using Catnip.Scripts.DI;
using Catnip.Scripts.Models;
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
                    G.Instance.thirdPersonCamera.transform.position.x, playerController.obj.position.y,
                    G.Instance.thirdPersonCamera.transform.position.z);
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

        // handle jump
        verticalVelocity += playerController.gravity * Time.deltaTime;
        playerController.characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    public void Rotate(Vector3 rotationVector) {
        if (playerController.personView == PersonView.First) {
            // Получаем угол поворота камеры по оси Y
            rotationY = G.Instance.firstPersonCamera.transform.eulerAngles.y;
            playerController.obj.rotation = Quaternion.Euler(0, rotationY, 0);
        }
        // if (personView == PersonView.First) {
        //     rotationY += rotationVector.x * rotationSpeed * Time.deltaTime;
        //     obj.localRotation = Quaternion.Euler(0, rotationY, 0);
        // }
    }

    public void Jump() {
        if (playerController.characterController.isGrounded) {
            verticalVelocity = playerController.jumpForce;
            playerController.animator.SetTrigger("Jump");
        }
    }

    public void SetRunning(bool running) {
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
        if (playerController.personView == PersonView.First) {
            G.Instance.firstPersonCamera.gameObject.SetActive(false);
            playerController.firstPersonFollow.gameObject.SetActive(false);
            G.Instance.thirdPersonCamera.gameObject.SetActive(true);
            playerController.thirdPersonFollow.gameObject.SetActive(true);
        } else if (playerController.personView == PersonView.Third) {
            G.Instance.thirdPersonCamera.gameObject.SetActive(false);
            playerController.thirdPersonFollow.gameObject.SetActive(false);
            G.Instance.firstPersonCamera.gameObject.SetActive(true);
            playerController.firstPersonFollow.gameObject.SetActive(true);
        }

        playerController.personView =
            playerController.personView == PersonView.First ? PersonView.Third : PersonView.First;
    }
}
}