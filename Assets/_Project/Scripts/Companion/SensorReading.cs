using UnityEngine;

namespace YazLab.Companion
{
    public readonly struct Detection
    {
        public readonly float Distance;
        public readonly float Angle;
        public readonly Collider Collider;

        public Detection(float distance, float angle, Collider collider)
        {
            Distance = distance;
            Angle = angle;
            Collider = collider;
        }

        public bool Exists => Collider != null;
    }

    public readonly struct SensorReading
    {
        public readonly Detection Wall;
        public readonly Detection Hazard;
        public readonly Detection Target;

        public SensorReading(Detection wall, Detection hazard, Detection target)
        {
            Wall = wall;
            Hazard = hazard;
            Target = target;
        }
    }
}
