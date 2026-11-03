using UnityEngine;

namespace YazLab.Companion.States
{
    public class FollowPlayerState : ICompanionState
    {
        private const float RepathInterval = 0.25f;
        private float nextRepathTime;

        public string Name => "FollowPlayer";

        public void Enter(CompanionController companion)
        {
            companion.Agent.isStopped = false;
            nextRepathTime = 0f;
        }

        public void Tick(CompanionController companion)
        {
            if (Time.time < nextRepathTime)
            {
                return;
            }

            nextRepathTime = Time.time + RepathInterval;
            float distance = Vector3.Distance(companion.transform.position, companion.Player.position);

            if (distance > companion.FollowDistance)
            {
                companion.Agent.SetDestination(companion.Player.position);
            }
            else
            {
                companion.Agent.ResetPath();
            }
        }

        public void Exit(CompanionController companion) { }
    }
}
