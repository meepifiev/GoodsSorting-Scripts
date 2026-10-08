using UnityEngine;

namespace _Project.Features.Building
{
    public readonly struct IsoBox
    {
        public readonly Vector3 Min;
        public readonly Vector3 Size;

        public IsoBox(Vector3 min, Vector3 size)
        {
            Min = min;
            Size = size;
        }

        public Vector3 Max => Min + Size;
        public Vector3 Center => Min + Size * 0.5f;
    }
}
