using System;
using UnityEngine;
using UnityEngine.AI;
using YazLab.Companion.States;
using YazLab.Interaction;

namespace YazLab.Companion
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(RaycastSensor))]
    public class CompanionController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float followDistance = 2.5f;
        [SerializeField] private float hazardStopDistance = 1.5f;
        [SerializeField] private float replanDelay = 0.3f;
        [SerializeField] private int maxReplans = 5;
        [SerializeField] private float reachDistance = 0.4f;
        [SerializeField] private float interactDistance = 1.6f;

        private RaycastSensor sensor;
        private ICompanionState currentState;

        public event Action<string> StateChanged;
        public event Action TaskCompleted;
        public event Action<string> TaskFailed;

        public NavMeshAgent Agent { get; private set; }
        public Transform Player => player;
        public float FollowDistance => followDistance;
        public float HazardStopDistance => hazardStopDistance;
        public float ReplanDelay => replanDelay;
        public int MaxReplans => maxReplans;
        public float ReachDistance => reachDistance;
        public float InteractDistance => interactDistance;
        public int ReplanCount { get; set; }
        public SensorReading LastReading { get; private set; }
        public IInteractable Task { get; private set; }
        public string CurrentStateName => currentState?.Name ?? "-";

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            sensor = GetComponent<RaycastSensor>();
        }

        private void Start()
        {
            ChangeState(new FollowPlayerState());
        }

        private void Update()
        {
            LastReading = sensor.Scan();
            currentState?.Tick(this);
        }

        public void ChangeState(ICompanionState newState)
        {
            currentState?.Exit(this);
            currentState = newState;
            Debug.Log($"[Companion] Yeni durum: {currentState.Name}");
            currentState.Enter(this);
            StateChanged?.Invoke(newState.Name);
        }

        public void Command(IInteractable task)
        {
            if (task == null || task.IsUsed || Task != null)
            {
                return;
            }

            Task = task;
            ReplanCount = 0;
            ChangeState(new MoveToTargetState());
        }

        public void CompleteTask()
        {
            Task = null;
            TaskCompleted?.Invoke();
            ChangeState(new FollowPlayerState());
        }

        public void FailTask(string reason)
        {
            Debug.LogWarning($"[Companion] Görev başarısız: {reason}");
            Task = null;
            TaskFailed?.Invoke(reason);
            ChangeState(new FollowPlayerState());
        }
    }
}
