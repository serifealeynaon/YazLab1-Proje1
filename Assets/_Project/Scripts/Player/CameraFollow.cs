using UnityEngine;

namespace YazLab.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -9f);
        [SerializeField] private float smoothTime = 0.2f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset,
                ref velocity, smoothTime);
            transform.LookAt(target.position);
        }
    }
}
