using UnityEngine;
using UnityEngine.AI;
using YazLab.Interaction;

namespace YazLab.Companion.States
{
    public class MoveToTargetState : ICompanionState
    {
        private const float PathCheckInterval = 0.5f;
        private float nextPathCheck;

        public string Name => "MoveToTarget";

        public void Enter(CompanionController companion)
        {
            companion.Agent.isStopped = false;
            nextPathCheck = Time.time + PathCheckInterval;
            RequestPath(companion);
        }

        public void Tick(CompanionController companion)
        {
            if (companion.Task == null)
            {
                return;
            }

            SensorReading reading = companion.LastReading;

            if (reading.Hazard.Exists
                && reading.Hazard.Distance <= companion.HazardStopDistance
                && reading.Hazard.Collider.GetComponentInParent<IRevealableObstacle>() is { } obstacle
                && !IsAlreadyAvoided(reading.Hazard.Collider))
            {
                companion.ChangeState(new ReplanState(obstacle));
                return;
            }

            if (companion.Agent.pathPending)
            {
                return;
            }

            bool targetInSight = reading.Target.Exists && reading.Target.Distance <= companion.InteractDistance;

            if (targetInSight || companion.Agent.remainingDistance <= companion.ReachDistance)
            {
                companion.ChangeState(new InteractState());
                return;
            }

            if (Time.time >= nextPathCheck)
            {
                nextPathCheck = Time.time + PathCheckInterval;

                if (companion.Agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    RequestPath(companion);
                }
            }
        }

        public void Exit(CompanionController companion) { }

        private static bool IsAlreadyAvoided(Collider hazard)
        {
            NavMeshObstacle navObstacle = hazard.GetComponentInParent<NavMeshObstacle>();
            return navObstacle != null && navObstacle.enabled;
        }

        private static void RequestPath(CompanionController companion)
        {
            var path = new NavMeshPath();
            bool found = companion.Agent.CalculatePath(companion.Task.InteractPosition, path);

            if (!found || path.status != NavMeshPathStatus.PathComplete)
            {
                companion.FailTask("Hedefe giden yol bulunamadı.");
                return;
            }

            companion.Agent.SetPath(path);
        }
    }
}
