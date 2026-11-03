using UnityEngine;
using UnityEngine.AI;

namespace YazLab.Interaction
{
    [RequireComponent(typeof(NavMeshObstacle))]
    public class DiscoverableCrate : MonoBehaviour, IRevealableObstacle
    {
        private NavMeshObstacle obstacle;

        private void Awake()
        {
            obstacle = GetComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.enabled = false;
        }

        public void Reveal()
        {
            obstacle.enabled = true;
        }
    }
}
