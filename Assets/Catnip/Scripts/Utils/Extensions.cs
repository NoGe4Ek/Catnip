using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

namespace Catnip.Scripts.Utils {
public static class Extensions {
    private static readonly Random Random = new();

    public static T GetRandom<T>(this List<T> list) {
        if (list == null || list.Count == 0) {
            throw new ArgumentException("Список пуст или равен null!");
        }

        return list[Random.Next(0, list.Count)];
    }

    public static T GetRandom<T>(this T[] array) {
        if (array == null || array.Length == 0) {
            throw new ArgumentException("Массив пуст или равен null!");
        }

        return array[Random.Next(0, array.Length)];
    }

    public static Vector3 GetRenderCenter(this GameObject obj) {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) {
            return renderer.bounds.center;
        }

        return obj.transform.position;
    }

    public static Vector3 GetLocalRenderCenter(this GameObject obj) {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) {
            // Преобразуем мировые координаты в локальные
            return obj.transform.InverseTransformPoint(renderer.bounds.center);
        }

        return Vector3.zero;
    }


    public static bool IsInLayer(this RaycastHit hit, LayerMask layerMask) {
        return layerMask == (layerMask | (1 << hit.collider.gameObject.layer));
    }

    // ПОИСК ВСЕХ ОБЪЕКТОВ ОПРЕДЕЛЕННОГО ТИПА СРЕДИ ДЕТЕЙ, РЕКУРСИВНО
    public static List<T> FindComponentsInChildrenRecursive<T>(this GameObject parent,
        bool findOnlyInActiveInHierarchy = true) where T : Component {
        List<T> components = new List<T>();
        FindComponentsRecursive(parent, ref components, findOnlyInActiveInHierarchy);
        return components;
    }

    private static void FindComponentsRecursive<T>(GameObject current, ref List<T> components,
        bool findOnlyInActiveInHierarchy) where T : Component {
        // Ищем только в активных объектах
        if (findOnlyInActiveInHierarchy && !current.activeInHierarchy)
            return;

        components.AddRange(current.GetComponents<T>());

        foreach (Transform child in current.transform) {
            FindComponentsRecursive(child.gameObject, ref components, findOnlyInActiveInHierarchy);
        }
    }

    // ПОИСК ПЕРВОГО ОБЪЕКТА ОПРЕДЕЛЕННОГО ТИПА СРЕДИ РОДИТЕЛЕЙ, РЕКУРСИВНО
    public static T FindComponentInParentRecursive<T>(this GameObject gameObject) where T : class {
        Transform current = gameObject.transform;

        while (current != null) {
            T component = current.GetComponent<T>();
            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }

    public static Vector2 GetMousePosition() {
        // Для мыши
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        // Для тачскрина
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        // Для геймпада - возвращаем центр экрана
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    public static void DrawSphereCastDebug(Vector3 origin, Vector3 direction, float radius, float distance,
        Color color) {
        // Рисуем линию направления
        Debug.DrawRay(origin, direction * distance, color, 2f);

        // Рисуем начальную сферу
        DrawDebugSphere(origin, radius, color);

        // Рисуем конечную сферу
        DrawDebugSphere(origin + direction * distance, radius, color);

        // Рисуем боковые линии для объема
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.right).normalized * radius;
        Debug.DrawLine(origin + perpendicular, origin + direction * distance + perpendicular, color, 2f);
        Debug.DrawLine(origin - perpendicular, origin + direction * distance - perpendicular, color, 2f);
    }

    private static void DrawDebugSphere(Vector3 center, float radius, Color color) {
        int segments = 12;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++) {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color, 2f);

            p1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            p2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
            Debug.DrawLine(p1, p2, color, 2f);

            p1 = center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
            p2 = center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color, 2f);
        }
    }

    // DEEP COPY
    //         GameObject tempCopy = new GameObject("TempCopy");
    // CopyAllComponentsRecursively(tileObject, tempCopy);
    // public static void CopyAllComponentsRecursively(GameObject source, GameObject destination) {
    //     if (source == null || destination == null)
    //         return;
    //
    //     // Копируем все компоненты текущего объекта (кроме NetworkIdentity)
    //     foreach (Component comp in source.GetComponents<Component>()) {
    //         if (!(comp is NetworkIdentity) && !(comp is NetworkTransformReliable) && !(comp is InteractDragComponent) &&
    //             !(comp is CombineInteractComponent)) {
    //             UnityEditorInternal.ComponentUtility.CopyComponent(comp);
    //             UnityEditorInternal.ComponentUtility.PasteComponentAsNew(destination);
    //         }
    //     }
    //
    //     // Рекурсивно обрабатываем всех детей
    //     for (int i = 0; i < source.transform.childCount; i++) {
    //         Transform sourceChild = source.transform.GetChild(i);
    //
    //         // Создаем соответствующий дочерний объект в destination
    //         GameObject destChild = new GameObject(sourceChild.name);
    //         destChild.transform.SetParent(destination.transform);
    //         destChild.transform.localPosition = sourceChild.localPosition;
    //         destChild.transform.localRotation = sourceChild.localRotation;
    //         destChild.transform.localScale = sourceChild.localScale;
    //
    //         // Копируем компоненты дочернего объекта
    //         CopyAllComponentsRecursively(sourceChild.gameObject, destChild);
    //     }
    // }

    public static void DrawSphereCastDebug(Ray ray, float sphereCastRadius, float interactionDistance,
        LayerMask holdableLayer) {
        // Выполняем SphereCast для дебага
        bool hasHit = Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, interactionDistance,
            holdableLayer);

        // Настройки визуализации
        Color rayColor = hasHit ? Color.green : Color.red;
        Color sphereColor = hasHit ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        float duration = 2f;

        // Рисуем луч
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, rayColor, duration);

        // Рисуем начальную сферу
        DrawDebugSphere(ray.origin, sphereCastRadius, sphereColor, duration);

        if (hasHit) {
            // Рисуем сферу в точке попадания
            DrawDebugSphere(hit.point, sphereCastRadius, Color.yellow, duration);

            // Рисуем нормаль
            Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.blue, duration);

            // Рисуем линию от начала до точки попадания
            Debug.DrawLine(ray.origin, hit.point, Color.cyan, duration);

            Debug.Log($"SphereCast hit: {hit.collider.name} at distance {hit.distance:F2}");
        } else {
            // Рисуем сферу в конце дистанции
            Vector3 endPoint = ray.origin + ray.direction * interactionDistance;
            DrawDebugSphere(endPoint, sphereCastRadius, sphereColor, duration);

            Debug.Log("SphereCast didn't hit anything");
        }
    }

    private static void DrawDebugSphere(Vector3 center, float radius, Color color, float duration) {
        // Рисуем три ортогональных круга для визуализации сферы
        DrawDebugCircle(center, Vector3.forward, Vector3.up, radius, color, duration);
        DrawDebugCircle(center, Vector3.up, Vector3.right, radius, color, duration);
        DrawDebugCircle(center, Vector3.right, Vector3.forward, radius, color, duration);
    }

    private static void DrawDebugCircle(Vector3 center, Vector3 normal, Vector3 axis, float radius, Color color,
        float duration) {
        int segments = 36;
        float angleStep = 360f / segments;

        Vector3 previousPoint = center + axis * radius;

        for (int i = 1; i <= segments; i++) {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point =
                center + (axis * Mathf.Cos(angle) + Vector3.Cross(normal, axis) * Mathf.Sin(angle)) * radius;
            Debug.DrawLine(previousPoint, point, color, duration);
            previousPoint = point;
        }
    }
}
}