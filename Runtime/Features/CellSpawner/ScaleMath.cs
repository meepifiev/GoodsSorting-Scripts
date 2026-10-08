using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public static class ScaleMath
    {
        public static Vector3 GetLocalScaleForWorldScale(Vector3 worldScale, Transform parent)
        {
            if (parent == null)
            {
                return worldScale;
            }

            return DivideScale(worldScale, parent.lossyScale);
        }

        public static Vector3 DivideScale(Vector3 scale, Vector3 parentScale)
        {
            return new Vector3(
                Divide(scale.x, parentScale.x),
                Divide(scale.y, parentScale.y),
                Divide(scale.z, parentScale.z));
        }

        private static float Divide(float value, float divisor)
        {
            if (Mathf.Approximately(divisor, 0f))
            {
                return value;
            }

            return value / divisor;
        }
    }
}
