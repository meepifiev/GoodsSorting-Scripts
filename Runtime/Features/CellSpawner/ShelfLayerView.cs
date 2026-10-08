using UnityEngine;

namespace _Project.Features.CellSpawner
{
    public class ShelfLayerView : MonoBehaviour
    {
        [SerializeField]
        private Transform[] _slots;

        public Transform[] Slots => _slots;
    }
}
