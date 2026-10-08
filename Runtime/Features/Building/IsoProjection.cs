using UnityEngine;

namespace _Project.Features.Building
{
    public class IsoProjection
    {
        private const float TileSize = 1f;
        private const float TileRatio = 0.5f;
        private const float TileHeight = 1f;

        private readonly float _tileSize = TileSize;
        private readonly float _tileRatio = TileRatio;
        private readonly float _tileHeight = TileHeight;

        public Vector2 IsoToScreen(Vector3 iso)
        {
            float x = (iso.x - iso.y) * _tileSize;
            float y = (iso.x + iso.y) * _tileSize * _tileRatio + iso.z * _tileHeight;
            return new Vector2(x, y);
        }

        public Vector3 ScreenToIso(Vector2 screen, float isoZ)
        {
            float groundY = screen.y - isoZ * _tileHeight;
            float sum = groundY / (_tileSize * _tileRatio);
            float difference = screen.x / _tileSize;
            float x = (sum + difference) * 0.5f;
            float y = (sum - difference) * 0.5f;
            return new Vector3(x, y, isoZ);
        }

        public Rect ScreenBounds(IsoBox box)
        {
            Vector3 min = box.Min;
            Vector3 max = box.Max;
            float left = IsoToScreen(new Vector3(min.x, max.y, min.z)).x;
            float right = IsoToScreen(new Vector3(max.x, min.y, min.z)).x;
            float bottom = IsoToScreen(new Vector3(min.x, min.y, min.z)).y;
            float top = IsoToScreen(new Vector3(max.x, max.y, max.z)).y;
            return Rect.MinMaxRect(left, bottom, right, top);
        }
    }
}
