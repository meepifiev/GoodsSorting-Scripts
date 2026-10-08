using System;
using System.Collections;
using _Project.Core.Abilities;
using _Project.Core.Pause;
using _Project.Core.UI;
using _Project.Features.Cameras;
using _Project.Features.Services.InactivityHintServices;
using UnityEngine;
using VContainer;

namespace _Project.Features.CellSpawner
{
    public class ShelfItemDragController : MonoBehaviour
    {
        private const int DragSortingOrderOffset = 10000;
        private const float DragStartThreshold = 8f;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private float _settleDuration = 0.16f;

        [SerializeField]
        private LayerMask _itemLayerMask = Physics2D.DefaultRaycastLayers;

        private ShelfItemView _draggedItem;
        private ShelfCellView _sourceCell;
        private Transform _sourceParent;
        private Vector3 _sourceLocalPosition;
        private Quaternion _sourceLocalRotation;
        private Vector3 _sourceLocalScale;
        private Vector3 _dragOffset;
        private Vector3 _pressScreenPosition;
        private ShelfItemView _pressedItem;
        private ShelfItemView _selectedItem;
        private Collider2D[] _draggedColliders;
        private DraggedRendererState[] _draggedRendererStates;
        private IInactivityHintService _inactivityHintService;
        private IAbilityRuntime _abilityRuntime;
        private IPauseService _pauseService;
        private IModalGate _modalGate;
        private LevelShelfSpawner _levelShelfSpawner;
        private IMainCameraProvider _cameraProvider;

        public event Action ItemPicked;
        public event Action ItemPlaced;

        private void Update()
        {
            if (_camera == null)
                return;

            if (_pauseService != null && _pauseService.IsPaused)
                return;

            if (_modalGate != null && _modalGate.IsOpen)
                return;

            if (_abilityRuntime != null && _abilityRuntime.IsBusy)
                return;

            if (Input.GetMouseButtonDown(0))
                HandlePointerDown();

            if (_draggedItem == null && _pressedItem != null && Input.GetMouseButton(0))
                TryStartDragFromPress();

            if (_draggedItem != null)
                Drag();

            if (Input.GetMouseButtonUp(0))
                HandlePointerUp();
        }

        [Inject]
        private void Construct(
            IInactivityHintService inactivityHintService,
            IAbilityRuntime abilityRuntime,
            IPauseService pauseService,
            IModalGate modalGate,
            LevelShelfSpawner levelShelfSpawner,
            IMainCameraProvider cameraProvider)
        {
            _inactivityHintService = inactivityHintService ?? throw new ArgumentNullException(nameof(inactivityHintService));
            _abilityRuntime = abilityRuntime ?? throw new ArgumentNullException(nameof(abilityRuntime));
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            _modalGate = modalGate ?? throw new ArgumentNullException(nameof(modalGate));
            _levelShelfSpawner = levelShelfSpawner ?? throw new ArgumentNullException(nameof(levelShelfSpawner));
            _cameraProvider = cameraProvider ?? throw new ArgumentNullException(nameof(cameraProvider));

            if (_camera == null)
                _camera = _cameraProvider.Camera;
        }

        private void HandlePointerDown()
        {
            _inactivityHintService.RegisterPlayerActivity();
            _pressScreenPosition = Input.mousePosition;
            _pressedItem = FindItemUnderPointer();
        }

        private void TryStartDragFromPress()
        {
            if (Vector3.Distance(_pressScreenPosition, Input.mousePosition) < DragStartThreshold)
                return;

            bool wasSelectedItemPressed = _selectedItem == _pressedItem;

            if (wasSelectedItemPressed)
            {
                _selectedItem = null;
            }
            else
            {
                SetSelectedItem(null);
            }

            BeginDrag(
                _pressedItem,
                wasSelectedItemPressed);
            _pressedItem = null;
        }

        private void HandlePointerUp()
        {
            if (_draggedItem != null)
            {
                EndDrag();
                return;
            }

            if (_pressedItem != null)
            {
                HandleItemClick(_pressedItem);
                _pressedItem = null;
                return;
            }

            if (_selectedItem != null)
                TryMoveSelectedItemToPointer();
        }

        private void HandleItemClick(ShelfItemView item)
        {
            if (_selectedItem == null)
            {
                SetSelectedItem(item);
                ItemPicked?.Invoke();
                return;
            }

            if (_selectedItem == item)
            {
                SetSelectedItem(null);
                return;
            }

            SetSelectedItem(item);
            ItemPicked?.Invoke();
        }

        private void TryMoveSelectedItemToPointer()
        {
            if (_selectedItem == null)
                return;

            if (_selectedItem.CanDrag == false)
            {
                SetSelectedItem(null);
                return;
            }

            ShelfItemView item = _selectedItem;
            ShelfCellView sourceCell = item.Cell;
            ShelfCellView targetCell = FindCellUnderPointer();
            Vector3 targetPoint = GetPointerWorldPosition(item.transform.position.z);

            if (sourceCell == null ||
                targetCell == null ||
                targetCell.TryAttachItemToNearestFreeFrontSlot(
                    item,
                    targetPoint,
                    true) == false)
            {
                return;
            }

            ItemPlaced?.Invoke();

            Collider2D[] colliders = item.GetComponentsInChildren<Collider2D>(true);
            SetCollidersEnabled(
                colliders,
                false);

            sourceCell.RemoveEmptyFrontLayers();
            sourceCell.RefreshLayerVisibility(true);

            if (targetCell != sourceCell)
                targetCell.RefreshLayerVisibility();

            ClearSelectedItemVisual(false);

            StartSettleAnimation(
                item.transform,
                colliders,
                Vector3.zero,
                () =>
                {
                    targetCell.TryResolveFrontLayerMatch();
                    _levelShelfSpawner.ScheduleDeadlockCheck();
                });
        }

        private void BeginDrag(
            ShelfItemView item,
            bool wasAlreadySelected)
        {
            _draggedItem = item;
            _sourceCell = item.Cell;
            _sourceParent = item.transform.parent;
            _sourceLocalPosition = item.Slot == item.transform.parent
                ? Vector3.zero
                : item.transform.localPosition;
            _sourceLocalRotation = item.transform.localRotation;
            _sourceLocalScale = item.SlotLocalScale;
            ShelfItemSelectionVisualView selectionVisualView = GetOrAddSelectionVisual(item);

            if (wasAlreadySelected)
                selectionVisualView.PrepareForDrag();

            _dragOffset = item.transform.position - GetPointerWorldPosition(item.transform.position.z);
            _draggedColliders = item.GetComponentsInChildren<Collider2D>(true);
            _draggedRendererStates = GetDraggedRendererStates(item);

            SetDraggedCollidersEnabled(false);
            item.transform.SetParent(null, true);

            if (wasAlreadySelected == false)
            {
                selectionVisualView.SetSelected(
                    true,
                    false,
                    true);
            }

            BringDraggedItemToFront(item);

            ItemPicked?.Invoke();
        }

        private void Drag()
        {
            Vector3 visualOffset = Vector3.zero;
            ShelfItemSelectionVisualView selectionVisualView = _draggedItem.GetComponent<ShelfItemSelectionVisualView>();

            if (selectionVisualView != null)
                visualOffset = selectionVisualView.CurrentDragOffset;

            _draggedItem.transform.position =
                GetPointerWorldPosition(_draggedItem.transform.position.z) + _dragOffset + visualOffset;
        }

        private void EndDrag()
        {
            bool renderersRestored = false;
            bool collidersHandledByAnimation = false;

            try
            {
                ShelfCellView targetCell = FindCellUnderPointer();
                Vector3 dropPosition = GetPointerWorldPosition(_draggedItem.transform.position.z);

                RestoreDraggedRenderers();
                renderersRestored = true;

                if (targetCell != null &&
                    targetCell.TryAttachItemToNearestFreeFrontSlot(
                        _draggedItem,
                        dropPosition,
                        true))
                {
                    _sourceCell.RemoveEmptyFrontLayers();
                    _sourceCell.RefreshLayerVisibility(true);
                    targetCell.RefreshLayerVisibility();
                    _sourceCell.TryResolveFrontLayerMatch();
                    StartSettleAnimation(
                        _draggedItem.transform,
                        _draggedColliders,
                        Vector3.zero,
                        () =>
                        {
                            targetCell.TryResolveFrontLayerMatch();
                            _levelShelfSpawner.ScheduleDeadlockCheck();
                        });
                    collidersHandledByAnimation = true;
                }
                else
                {
                    Vector3 draggedWorldScale = _draggedItem.transform.lossyScale;
                    ReturnDraggedItem(true);
                    _draggedItem.transform.localScale = ScaleMath.GetLocalScaleForWorldScale(
                        draggedWorldScale,
                        _draggedItem.transform.parent);
                    _sourceCell.RefreshLayerVisibility();
                    StartSettleAnimation(
                        _draggedItem.transform,
                        _draggedColliders,
                        _sourceLocalPosition,
                        null);
                    collidersHandledByAnimation = true;
                }
            }
            finally
            {
                if (renderersRestored == false)
                    RestoreDraggedRenderers();

                if (collidersHandledByAnimation == false)
                    SetDraggedCollidersEnabled(true);

                SetItemSelectionVisual(
                    _draggedItem,
                    false,
                    false);
                ClearDragState();

                ItemPlaced?.Invoke();
            }
        }

        private void SetSelectedItem(ShelfItemView item)
        {
            if (_selectedItem == item)
                return;

            ClearSelectedItemVisual(true);
            _selectedItem = item;
            SetItemSelectionVisual(
                _selectedItem,
                true,
                true);
        }

        private void ClearSelectedItemVisual(bool applyPositionToTransform)
        {
            SetItemSelectionVisual(
                _selectedItem,
                false,
                applyPositionToTransform);
            _selectedItem = null;
        }

        private void SetItemSelectionVisual(
            ShelfItemView item,
            bool isSelected,
            bool applyPositionToTransform)
        {
            if (item == null)
                return;

            GetOrAddSelectionVisual(item).SetSelected(
                isSelected,
                applyPositionToTransform);
        }

        private ShelfItemSelectionVisualView GetOrAddSelectionVisual(ShelfItemView item)
        {
            ShelfItemSelectionVisualView selectionVisualView = item.GetComponent<ShelfItemSelectionVisualView>();

            if (selectionVisualView == null)
                selectionVisualView = item.gameObject.AddComponent<ShelfItemSelectionVisualView>();

            return selectionVisualView;
        }

        private ShelfItemView FindItemUnderPointer()
        {
            Vector3 pointerPosition = GetPointerWorldPosition(0f);
            Collider2D[] hits = Physics2D.OverlapPointAll(pointerPosition, _itemLayerMask);
            ShelfItemView bestItem = null;
            int bestSortingOrder = int.MinValue;

            foreach (Collider2D hit in hits)
            {
                ShelfItemView item = hit.GetComponentInParent<ShelfItemView>();

                if (item == null)
                    continue;

                if (item.CanDrag == false)
                    continue;

                int sortingOrder = GetHighestSortingOrder(item);

                if (sortingOrder <= bestSortingOrder)
                    continue;

                bestItem = item;
                bestSortingOrder = sortingOrder;
            }

            return bestItem;
        }

        private ShelfCellView FindCellUnderPointer()
        {
            ShelfItemView item = FindItemUnderPointer();

            if (item != null && item != _draggedItem)
                return item.Cell;

            Vector3 pointerPosition = GetPointerWorldPosition(0f);
            ShelfCellView[] cells = FindObjectsOfType<ShelfCellView>();
            ShelfCellView bestCell = null;
            float bestDistance = float.MaxValue;

            foreach (ShelfCellView cell in cells)
            {
                if (cell.ContainsWorldPoint(pointerPosition) == false)
                    continue;

                float distance = Vector2.Distance(pointerPosition, cell.transform.position);

                if (distance >= bestDistance)
                    continue;

                bestCell = cell;
                bestDistance = distance;
            }

            return bestCell;
        }

        private Vector3 GetPointerWorldPosition(float worldZ)
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = worldZ - _camera.transform.position.z;
            return _camera.ScreenToWorldPoint(screenPosition);
        }

        private void ReturnDraggedItem(bool preserveWorldPosition)
        {
            Vector3 worldPosition = _draggedItem.transform.position;

            _draggedItem.transform.SetParent(_sourceParent, false);
            _draggedItem.transform.localPosition = _sourceLocalPosition;
            _draggedItem.transform.localRotation = _sourceLocalRotation;
            _draggedItem.transform.localScale = _sourceLocalScale;

            if (preserveWorldPosition)
                _draggedItem.transform.position = worldPosition;
        }

        private void StartSettleAnimation(
            Transform item,
            Collider2D[] colliders,
            Vector3 targetLocalPosition,
            Action onComplete)
        {
            StartCoroutine(SettleItem(
                item,
                colliders,
                targetLocalPosition,
                onComplete));
        }

        private IEnumerator SettleItem(
            Transform item,
            Collider2D[] colliders,
            Vector3 targetLocalPosition,
            Action onComplete)
        {
            if (item == null)
                yield break;

            Vector3 startLocalPosition = item.localPosition;
            float elapsed = 0f;

            while (elapsed < _settleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _settleDuration);
                float easedProgress = progress * progress * (3f - 2f * progress);

                item.localPosition = Vector3.LerpUnclamped(
                    startLocalPosition,
                    targetLocalPosition,
                    easedProgress);

                yield return null;
            }

            item.localPosition = targetLocalPosition;
            SetCollidersEnabled(
                colliders,
                true);
            onComplete?.Invoke();
        }

        private void ClearDragState()
        {
            _draggedItem = null;
            _sourceCell = null;
            _sourceParent = null;
            _draggedColliders = null;
            _draggedRendererStates = null;
        }

        private void SetDraggedCollidersEnabled(bool isEnabled)
        {
            SetCollidersEnabled(
                _draggedColliders,
                isEnabled);
        }

        private void SetCollidersEnabled(
            Collider2D[] colliders,
            bool isEnabled)
        {
            if (colliders == null)
                return;

            foreach (Collider2D itemCollider in colliders)
            {
                if (itemCollider != null)
                    itemCollider.enabled = isEnabled;
            }
        }

        private void BringDraggedItemToFront(ShelfItemView item)
        {
            SpriteRenderer[] renderers = item.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                    continue;

                spriteRenderer.sortingOrder += DragSortingOrderOffset;
            }
        }

        private DraggedRendererState[] GetDraggedRendererStates(ShelfItemView item)
        {
            SpriteRenderer[] renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
            DraggedRendererState[] states = new DraggedRendererState[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (ShelfItemShadowView.IsShadowRenderer(renderers[i]))
                    continue;

                states[i] = new DraggedRendererState(
                    renderers[i],
                    renderers[i].color,
                    renderers[i].sortingOrder);
            }

            return states;
        }

        private void RestoreDraggedRenderers()
        {
            if (_draggedRendererStates == null)
                return;

            foreach (DraggedRendererState state in _draggedRendererStates)
            {
                if (state.Renderer == null)
                    continue;

                state.Renderer.color = state.Color;
                state.Renderer.sortingOrder = state.SortingOrder;
            }
        }

        private int GetHighestSortingOrder(ShelfItemView item)
        {
            SpriteRenderer[] renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
            int sortingOrder = int.MinValue;

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                if (ShelfItemShadowView.IsShadowRenderer(spriteRenderer))
                    continue;

                sortingOrder = Mathf.Max(sortingOrder, spriteRenderer.sortingOrder);
            }

            return sortingOrder;
        }

        private readonly struct DraggedRendererState
        {
            public DraggedRendererState(
                SpriteRenderer renderer,
                Color color,
                int sortingOrder)
            {
                Renderer = renderer;
                Color = color;
                SortingOrder = sortingOrder;
            }

            public SpriteRenderer Renderer { get; }
            public Color Color { get; }
            public int SortingOrder { get; }
        }
    }
}
