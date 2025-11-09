// https://github.com/krockot/Unity-TaskManager/blob/master/TaskManager.cs
// TaskManager.cs
//
// This is a convenient coroutine API for Unity.
//
// Example usage:
//   IEnumerator MyAwesomeTask()
//   {
//       while(true) {
//           // ...
//           yield return null;
//      }
//   }
//
//   IEnumerator TaskKiller(float delay, Task t)
//   {
//       yield return new WaitForSeconds(delay);
//       t.Stop();
//   }
//
//   // From anywhere
//   Task my_task = new Task(MyAwesomeTask());
//   new Task(TaskKiller(5, my_task));
//
// The code above will schedule MyAwesomeTask() and keep it running
// concurrently until either it terminates on its own, or 5 seconds elapses
// and triggers the TaskKiller Task that was created.
//
// Note that to facilitate this API's behavior, a "TaskManager" GameObject is
// created lazily on first use of the Task API and placed in the scene root
// with the internal TaskManager component attached. All coroutine dispatch
// for Tasks is done through this component.
// 

using System.Collections;
using UnityEngine;
namespace Catnip.Scripts.Managers {
/// A Task object represents a coroutine.  Tasks can be started, paused, and stopped.
/// It is an error to attempt to start a task that has been stopped or which has
/// naturally terminated.
public class Task {
    private readonly TaskManager.TaskState _task;

    /// Returns true if and only if the coroutine is running.  Paused tasks
    /// are considered to be running.
    public bool Running => _task.Running;

    /// Returns true if and only if the coroutine is currently paused.
    public bool Paused => _task.Paused;

    /// Delegate for termination subscribers.  manual is true if and only if
    /// the coroutine was stopped with an explicit call to Stop().
    public delegate void FinishedHandler(bool manual);

    /// Termination event.  Triggered when the coroutine completes execution.
    public event FinishedHandler Finished;

    /// Creates a new Task object for the given coroutine.
    ///
    /// If autoStart is true (default) the task is automatically started
    /// upon construction.
    public Task(IEnumerator c, bool autoStart = true) {
        _task = TaskManager.CreateTask(c);
        _task.Finished += TaskFinished;
        if (autoStart)
            Start();
    }

    /// Begins execution of the coroutine
    // ReSharper disable once MemberCanBePrivate.Global
    public void Start() {
        _task.Start();
    }

    /// Discontinues execution of the coroutine at its next yield.
    public void Stop() {
        _task.Stop();
    }

    public void Pause() {
        _task.Pause();
    }

    public void Unpause() {
        _task.Unpause();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void TaskFinished(bool manual) {
        var handler = Finished;
        handler?.Invoke(manual);
    }
}

internal class TaskManager : MonoBehaviour {
    private static TaskManager Instance { get; set; }

    private void Awake() {
        Instance = this;
    }

    public class TaskState {
        public bool Running { get; private set; }

        public bool Paused { get; private set; }

        public delegate void FinishedHandler(bool manual);

        public event FinishedHandler Finished;

        private readonly IEnumerator _coroutine;
        private bool _stopped;

        public TaskState(IEnumerator c) {
            _coroutine = c;
        }

        public void Pause() {
            Paused = true;
        }

        public void Unpause() {
            Paused = false;
        }

        public void Start() {
            Running = true;
            Instance.StartCoroutine(CallWrapper());
        }

        public void Stop() {
            _stopped = true;
            Running = false;
        }

        private IEnumerator CallWrapper() {
            yield return null;
            var e = _coroutine;
            while (Running) {
                if (Paused)
                    yield return null;
                else {
                    if (e != null && e.MoveNext()) {
                        yield return e.Current;
                    } else {
                        Running = false;
                    }
                }
            }

            var handler = Finished;
            handler?.Invoke(_stopped);
        }
    }

    public static TaskState CreateTask(IEnumerator coroutine) {
        // Если необходимо управлять созданием TaskManager лениво
        // if(singleton == null) {
        // 	GameObject go = new GameObject("TaskManager");
        // 	singleton = go.AddComponent<TaskManager>();
        // }
        return new TaskState(coroutine);
    }
}
}
