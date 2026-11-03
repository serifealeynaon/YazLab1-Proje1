using UnityEngine;

namespace YazLab.Companion
{
    public class RaycastSensor : MonoBehaviour
    {
        [SerializeField] private float range = 3f;
        [SerializeField] private float halfFov = 45f;
        [SerializeField] private int rayCount = 7;
        [SerializeField] private float eyeHeight = 0.2f;
        [SerializeField] private LayerMask wallMask;
        [SerializeField] private LayerMask hazardMask;
        [SerializeField] private LayerMask targetMask;

        public SensorReading Scan()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            LayerMask allMasks = wallMask | hazardMask | targetMask;

            Detection wall = default;
            Detection hazard = default;
            Detection target = default;

            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount == 1 ? 0.5f : (float)i / (rayCount - 1);
                float angle = Mathf.Lerp(-halfFov, halfFov, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

                if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, allMasks))
                {
                    Debug.DrawRay(origin, direction * range, Color.green);
                    continue;
                }

                Debug.DrawRay(origin, direction * hit.distance, Color.red);
                var detection = new Detection(hit.distance, angle, hit.collider);
                int layerBit = 1 << hit.collider.gameObject.layer;

                if ((layerBit & targetMask) != 0)
                {
                    target = Closest(target, detection);
                }
                else if ((layerBit & hazardMask) != 0)
                {
                    hazard = Closest(hazard, detection);
                }
                else
                {
                    wall = Closest(wall, detection);
                }
            }

            return new SensorReading(wall, hazard, target);
        }

        private static Detection Closest(Detection current, Detection candidate)
        {
            return !current.Exists || candidate.Distance < current.Distance ? candidate : current;
        }
    }
}
