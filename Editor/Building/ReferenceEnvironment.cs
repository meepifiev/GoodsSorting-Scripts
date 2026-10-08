using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class ReferenceEnvironment
    {
        public class TilemapLayer
        {
            public string Name;
            public bool Active;
            public Vector3 LocalPosition;
            public UnityYamlNode Tilemap;
            public UnityYamlNode Renderer;
        }

        public class DecorSprite
        {
            public string Name;
            public Vector3 WorldPosition;
            public Vector3 WorldScale;
            public string SpriteGuid;
            public int SortingOrder;
            public Color Color;
            public bool FlipX;
            public bool FlipY;
        }

        private const int GameObjectClass = 1;
        private const int TransformClass = 4;
        private const int MonoBehaviourClass = 114;
        private const int SpriteRendererClass = 212;
        private const int GridClass = 156049354;
        private const int TilemapClass = 1839735485;
        private const int TilemapRendererClass = 483693784;
        private const string GridObjectName = "GridEnvi";

        private readonly Dictionary<long, UnityYamlDocument> _byId = new Dictionary<long, UnityYamlDocument>();
        private readonly Dictionary<long, UnityYamlDocument> _transformByGameObject = new Dictionary<long, UnityYamlDocument>();
        private readonly Dictionary<long, UnityYamlDocument> _tilemapByGameObject = new Dictionary<long, UnityYamlDocument>();
        private readonly Dictionary<long, UnityYamlDocument> _rendererByGameObject = new Dictionary<long, UnityYamlDocument>();
        private readonly Dictionary<long, UnityYamlDocument> _gridByGameObject = new Dictionary<long, UnityYamlDocument>();
        private readonly Dictionary<long, UnityYamlDocument> _spriteByGameObject = new Dictionary<long, UnityYamlDocument>();
        private readonly HashSet<long> _scriptedGameObjects = new HashSet<long>();

        public UnityYamlNode Grid { get; private set; }
        public List<TilemapLayer> Layers { get; } = new List<TilemapLayer>();
        public List<DecorSprite> Decor { get; } = new List<DecorSprite>();

        public void Load(List<UnityYamlDocument> sceneDocuments)
        {
            Index(sceneDocuments);
            UnityYamlDocument gridObject = FindGridObject(sceneDocuments);
            Grid = _gridByGameObject[gridObject.FileId].Fields;
            UnityYamlDocument gridTransform = _transformByGameObject[gridObject.FileId];

            Layers.Clear();
            Decor.Clear();

            foreach (UnityYamlNode childReference in gridTransform.Fields.GetOrEmpty("m_Children").Items)
            {
                long childTransformId = childReference.ToReference().FileId;

                if (_byId.TryGetValue(childTransformId, out UnityYamlDocument childTransform) == false)
                {
                    continue;
                }

                long childObjectId = childTransform.Fields.GetReference("m_GameObject").FileId;

                if (_byId.TryGetValue(childObjectId, out UnityYamlDocument childObject) == false)
                {
                    continue;
                }

                if (_tilemapByGameObject.TryGetValue(childObjectId, out UnityYamlDocument tilemap))
                {
                    _rendererByGameObject.TryGetValue(childObjectId, out UnityYamlDocument renderer);

                    Layers.Add(new TilemapLayer
                    {
                        Name = childObject.Fields.GetString("m_Name", "Tilemap"),
                        Active = childObject.Fields.GetInt("m_IsActive", 1) != 0,
                        LocalPosition = childTransform.Fields.GetVector3("m_LocalPosition"),
                        Tilemap = tilemap.Fields,
                        Renderer = renderer?.Fields
                    });
                    continue;
                }

                CollectDecor(childTransform, childObject, Vector3.zero, Vector3.one, true);
            }
        }

        private void Index(List<UnityYamlDocument> sceneDocuments)
        {
            _byId.Clear();
            _transformByGameObject.Clear();
            _tilemapByGameObject.Clear();
            _rendererByGameObject.Clear();
            _gridByGameObject.Clear();
            _spriteByGameObject.Clear();
            _scriptedGameObjects.Clear();

            foreach (UnityYamlDocument document in sceneDocuments)
            {
                _byId[document.FileId] = document;
                long owner = document.Fields.GetReference("m_GameObject").FileId;

                switch (document.ClassId)
                {
                    case TransformClass:
                        _transformByGameObject[owner] = document;
                        break;
                    case TilemapClass:
                        _tilemapByGameObject[owner] = document;
                        break;
                    case TilemapRendererClass:
                        _rendererByGameObject[owner] = document;
                        break;
                    case GridClass:
                        _gridByGameObject[owner] = document;
                        break;
                    case SpriteRendererClass:
                        _spriteByGameObject[owner] = document;
                        break;
                    case MonoBehaviourClass:
                        _scriptedGameObjects.Add(owner);
                        break;
                }
            }
        }

        private UnityYamlDocument FindGridObject(List<UnityYamlDocument> sceneDocuments)
        {
            foreach (UnityYamlDocument document in sceneDocuments)
            {
                if (document.ClassId == GameObjectClass && document.Fields.GetString("m_Name") == GridObjectName)
                {
                    return document;
                }
            }

            throw new InvalidOperationException(GridObjectName);
        }

        private void CollectDecor(
            UnityYamlDocument transform,
            UnityYamlDocument gameObject,
            Vector3 parentPosition,
            Vector3 parentScale,
            bool parentActive)
        {
            Vector3 localPosition = transform.Fields.GetVector3("m_LocalPosition");
            Vector3 localScale = transform.Fields.GetVector3("m_LocalScale", Vector3.one);
            Vector3 worldPosition = parentPosition + Vector3.Scale(parentScale, localPosition);
            Vector3 worldScale = Vector3.Scale(parentScale, localScale);
            bool active = parentActive && gameObject.Fields.GetInt("m_IsActive", 1) != 0;
            long objectId = gameObject.FileId;

            if (active
                && _scriptedGameObjects.Contains(objectId) == false
                && _spriteByGameObject.TryGetValue(objectId, out UnityYamlDocument spriteRenderer))
            {
                UnityYamlReference sprite = spriteRenderer.Fields.GetReference("m_Sprite");

                if (sprite.IsExternal)
                {
                    Decor.Add(new DecorSprite
                    {
                        Name = gameObject.Fields.GetString("m_Name", "Decor"),
                        WorldPosition = worldPosition,
                        WorldScale = worldScale,
                        SpriteGuid = sprite.Guid,
                        SortingOrder = spriteRenderer.Fields.GetInt("m_SortingOrder"),
                        Color = ReadColor(spriteRenderer.Fields),
                        FlipX = spriteRenderer.Fields.GetInt("m_FlipX") != 0,
                        FlipY = spriteRenderer.Fields.GetInt("m_FlipY") != 0
                    });
                }
            }

            foreach (UnityYamlNode childReference in transform.Fields.GetOrEmpty("m_Children").Items)
            {
                long childTransformId = childReference.ToReference().FileId;

                if (_byId.TryGetValue(childTransformId, out UnityYamlDocument childTransform) == false)
                {
                    continue;
                }

                long childObjectId = childTransform.Fields.GetReference("m_GameObject").FileId;

                if (_byId.TryGetValue(childObjectId, out UnityYamlDocument childObject) == false)
                {
                    continue;
                }

                CollectDecor(childTransform, childObject, worldPosition, worldScale, active);
            }
        }

        private Color ReadColor(UnityYamlNode rendererFields)
        {
            UnityYamlNode color = rendererFields["m_Color"];

            if (color == null || color.IsMap == false)
            {
                return Color.white;
            }

            return new Color(color.GetFloat("r", 1f), color.GetFloat("g", 1f), color.GetFloat("b", 1f), color.GetFloat("a", 1f));
        }
    }
}
