using System;
using System.Collections.Generic;
namespace Catnip.Scripts.Managers {
public enum EventKey {
    // Session manager
    BootCompleted,
    LocalPlayerReady,
    NewPlayerConnected,
    TimeTick,

    // Input manager
    PrimaryInteract,
    SecondaryInteract,
    
    // CustomerManager
    PurchaseRequestTick,
    PurchaseRequestExpire
}

/**
 * Usage example
 *
 * EventManager.TriggerEvent<PlayerController>(EventKey.NewPlayerConnected, playerController);
 *
 * EventManager.AddListener<PlayerController>(EventKey.LocalPlayerReady, controller => {
            for (var i = 0; i < controller.playerId; i++) {
                if (!knownPlayers.ContainsKey(i))
                    AddKnownPlayer(i);
            }
        });
 */
public static class EventManager {
    // ===== Базовая версия (без данных) =====
    private static readonly Dictionary<EventKey, Action> Events = new();

    public static void AddListener(EventKey eventKey, Action handler) {
        if (Events.TryGetValue(eventKey, out var existingEvent)) {
            existingEvent += handler;
            Events[eventKey] = existingEvent;
        } else {
            Events.Add(eventKey, handler);
        }
    }

    public static void RemoveListener(EventKey eventKey, Action handler) {
        if (!Events.TryGetValue(eventKey, out var existingEvent)) return;

        existingEvent -= handler;
        if (existingEvent == null)
            Events.Remove(eventKey);
        else
            Events[eventKey] = existingEvent;
    }

    public static void TriggerEvent(EventKey eventKey) {
        if (Events.TryGetValue(eventKey, out var thisEvent)) {
            thisEvent?.Invoke();
        }
    }

    // ===== Generic-версия (с передачей данных) =====
    private static readonly Dictionary<EventKey, Delegate> GenericEvents = new();

    public static void AddListener<T>(EventKey eventKey, Action<T> handler) {
        if (GenericEvents.TryGetValue(eventKey, out var existingEvent)) {
            GenericEvents[eventKey] = Delegate.Combine(existingEvent, handler);
        } else {
            GenericEvents.Add(eventKey, handler);
        }
    }

    public static void AddListener<T1, T2>(EventKey eventKey, Action<T1, T2> handler) {
        if (GenericEvents.TryGetValue(eventKey, out var existingEvent)) {
            GenericEvents[eventKey] = Delegate.Combine(existingEvent, handler);
        } else {
            GenericEvents.Add(eventKey, handler);
        }
    }

    public static void AddListener<T1, T2, T3>(EventKey eventKey, Action<T1, T2, T3> handler) {
        if (GenericEvents.TryGetValue(eventKey, out var existingEvent)) {
            GenericEvents[eventKey] = Delegate.Combine(existingEvent, handler);
        } else {
            GenericEvents.Add(eventKey, handler);
        }
    }

    public static void RemoveListener<T>(EventKey eventKey, Action<T> handler) {
        if (!GenericEvents.TryGetValue(eventKey, out var existingEvent)) return;

        var newEvent = Delegate.Remove(existingEvent, handler);
        if (newEvent == null)
            GenericEvents.Remove(eventKey);
        else
            GenericEvents[eventKey] = newEvent;
    }

    public static void TriggerEvent<T>(EventKey eventKey, T eventData) {
        if (GenericEvents.TryGetValue(eventKey, out var thisEvent)) {
            (thisEvent as Action<T>)?.Invoke(eventData);
        }
    }

    public static void TriggerEvent<T1, T2>(EventKey eventKey, T1 eventData1, T2 eventData2) {
        if (GenericEvents.TryGetValue(eventKey, out var thisEvent)) {
            (thisEvent as Action<T1, T2>)?.Invoke(eventData1, eventData2);
        }
    }

    public static void TriggerEvent<T1, T2, T3>(EventKey eventKey, T1 eventData1, T2 eventData2, T3 eventData3) {
        if (GenericEvents.TryGetValue(eventKey, out var thisEvent)) {
            (thisEvent as Action<T1, T2, T3>)?.Invoke(eventData1, eventData2, eventData3);
        }
    }

    // ===== Очистка (вызывать при смене сцены/завершении игры) =====
    public static void Clear() {
        Events.Clear();
        GenericEvents.Clear();
    }
}
}
