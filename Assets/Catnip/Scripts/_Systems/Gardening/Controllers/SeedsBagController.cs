using System.Collections;
using Catnip.Scripts.Controllers;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;
using Extensions = Catnip.Scripts.Utils.Extensions;
namespace Catnip.Scripts._Systems.Gardening {
public class SeedsBagController : NetworkBehaviour, IUsable {
    [Header("Seeds Pouring Settings")]
    [SerializeField] private float sphereCastRadius = 0.2f;
    [SerializeField] private float pourDistance = 2f;
    [SerializeField] private LayerMask potLayerMask;
    [SerializeField] private VisualEffect pourVfx;

    private float pourAnimationDelay = 0.5f;
    [SyncVar] private bool isPouring;

    public void ClientUse(Ray ray) {
        if (!isPouring) {
            StartCoroutine(PourRoutine());
        }
    }
    
    public void ServerUse(Ray ray) {
        // unused
    }

    [Command]
    public void CmdServerUseInternal() {
        PourOutSeedsInternal();
    }
    
    private IEnumerator PourRoutine() {
        isPouring = true;

        // 1. Переворачиваем transform по X на +160
        yield return StartCoroutine(RotateBag(Vector3.zero, new Vector3(160f, 0f, 0f), 0.3f));

        // 2. Запускаем VFX анимацию
        pourVfx.SendEvent("OnPlay");
        // или pourVFX.Play();

        // 3. Ждем полсекунды перед кастом
        yield return new WaitForSeconds(pourAnimationDelay);

        // 4. Выполняем логику с кастом сферы
        CmdServerUseInternal();

        // 5. Ждем еще немного перед завершением
        yield return new WaitForSeconds(0.2f);

        // 6. Отключаем VFX
        pourVfx.SendEvent("OnStop");

        // 7. Возвращаем transform обратно
        yield return StartCoroutine(RotateBag(transform.localEulerAngles, Vector3.zero, 0.3f));

        isPouring = false;
    }

    private IEnumerator RotateBag(Vector3 fromRotation, Vector3 toRotation, float duration) {
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Плавная интерполяция поворота
            transform.localEulerAngles = Vector3.Lerp(fromRotation, toRotation, t);

            yield return null;
        }

        // Гарантируем точный конечный поворот
        transform.localEulerAngles = toRotation;
    }

    private void PourOutSeedsInternal() {
        Vector3 castStart = transform.position;
        Vector3 castDirection = Vector3.down;

        Extensions.DrawSphereCastDebug(castStart, castDirection, sphereCastRadius, pourDistance, Color.red);

        Debug.DrawRay(castStart, castDirection * pourDistance, Color.red, 2f);

        if (Physics.SphereCast(castStart, sphereCastRadius, castDirection, out RaycastHit hit, pourDistance, potLayerMask)) {
            // Check if we hit a pot
            // todo make it recursive in all
            PotController potController = hit.collider.GetComponentInParent<PotController>();
            if (potController != null) {
                // Call method to add soil to the pot
                potController.AddSeeds();
            }
        }
    }
}
}
