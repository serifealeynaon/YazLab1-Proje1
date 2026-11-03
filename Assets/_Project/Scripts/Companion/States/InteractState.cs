using UnityEngine;
using YazLab.Interaction;

namespace YazLab.Companion.States
{
    public class InteractState : ICompanionState
    {
        private const float MaxDuration = 2f;
        private const float TurnSpeed = 240f;
        private float deadline;

        public string Name => "Interact";

        public void Enter(CompanionController companion)
        {
            companion.Agent.ResetPath();
            deadline = Time.time + MaxDuration;
        }

        public void Tick(CompanionController companion)
        {
            Detection target = companion.LastReading.Target;

            if (target.Exists && target.Distance <= companion.InteractDistance
                && target.Collider.GetComponentInParent<IInteractable>() == companion.Task)
            {
                companion.Task.Interact();
                companion.CompleteTask();
                return;
            }

            if (Time.time > deadline)
            {
                companion.FailTask("Şalter sensörle doğrulanamadı.");
                return;
            }

            Vector3 lookTarget = companion.Task.LookPosition - companion.transform.position;
            lookTarget.y = 0f;

            if (lookTarget.sqrMagnitude > 0.001f)
            {
                companion.transform.rotation = Quaternion.RotateTowards(companion.transform.rotation,
                    Quaternion.LookRotation(lookTarget), TurnSpeed * Time.deltaTime);
            }
        }

        public void Exit(CompanionController companion) { }
    }
}
