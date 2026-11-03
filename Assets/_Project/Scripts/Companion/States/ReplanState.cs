using UnityEngine;
using YazLab.Interaction;

namespace YazLab.Companion.States
{
    public class ReplanState : ICompanionState
    {
        private readonly IRevealableObstacle obstacle;
        private float resumeTime;

        public ReplanState(IRevealableObstacle obstacle)
        {
            this.obstacle = obstacle;
        }

        public string Name => "Replan";

        public void Enter(CompanionController companion)
        {
            companion.Agent.isStopped = true;
            companion.ReplanCount++;
            obstacle.Reveal();
            resumeTime = Time.time + companion.ReplanDelay;
        }

        public void Tick(CompanionController companion)
        {
            if (Time.time < resumeTime)
            {
                return;
            }

            if (companion.ReplanCount > companion.MaxReplans)
            {
                companion.FailTask("Engel aşılamadı.");
                return;
            }

            companion.ChangeState(new MoveToTargetState());
        }

        public void Exit(CompanionController companion)
        {
            companion.Agent.isStopped = false;
        }
    }
}
