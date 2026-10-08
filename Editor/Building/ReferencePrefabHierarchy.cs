using System.Collections.Generic;

namespace _Project.Editor.Building
{
    public class ReferencePrefabHierarchy
    {
        public class Node
        {
            public string Name;
            public bool Active;
            public UnityYamlNode Transform;
            public UnityYamlNode SpriteRenderer;
            public UnityYamlNode Grid;
            public UnityYamlNode Tilemap;
            public UnityYamlNode TilemapRenderer;
            public readonly List<Node> Children = new List<Node>();
        }

        private const int GameObjectClass = 1;
        private const int TransformClass = 4;
        private const int SpriteRendererClass = 212;
        private const int GridClass = 156049354;
        private const int TilemapClass = 1839735485;
        private const int TilemapRendererClass = 483693784;

        public Node Root { get; private set; }

        public void Build(List<UnityYamlDocument> documents)
        {
            Dictionary<long, Node> nodesByGameObject = new Dictionary<long, Node>();
            Dictionary<long, UnityYamlDocument> transforms = new Dictionary<long, UnityYamlDocument>();

            foreach (UnityYamlDocument document in documents)
            {
                if (document.ClassId == GameObjectClass)
                {
                    nodesByGameObject[document.FileId] = new Node
                    {
                        Name = document.Fields.GetString("m_Name", "Item"),
                        Active = document.Fields.GetInt("m_IsActive", 1) != 0
                    };
                }
                else if (document.ClassId == TransformClass)
                {
                    transforms[document.FileId] = document;
                }
            }

            foreach (UnityYamlDocument document in documents)
            {
                long owner = document.Fields.GetReference("m_GameObject").FileId;

                if (nodesByGameObject.TryGetValue(owner, out Node node) == false)
                {
                    continue;
                }

                switch (document.ClassId)
                {
                    case TransformClass:
                        node.Transform = document.Fields;
                        break;
                    case SpriteRendererClass:
                        node.SpriteRenderer = document.Fields;
                        break;
                    case GridClass:
                        node.Grid = document.Fields;
                        break;
                    case TilemapClass:
                        node.Tilemap = document.Fields;
                        break;
                    case TilemapRendererClass:
                        node.TilemapRenderer = document.Fields;
                        break;
                }
            }

            Dictionary<long, Node> nodesByTransform = new Dictionary<long, Node>();

            foreach (KeyValuePair<long, UnityYamlDocument> pair in transforms)
            {
                long owner = pair.Value.Fields.GetReference("m_GameObject").FileId;

                if (nodesByGameObject.TryGetValue(owner, out Node node))
                {
                    nodesByTransform[pair.Key] = node;
                }
            }

            foreach (KeyValuePair<long, UnityYamlDocument> pair in transforms)
            {
                Node node = nodesByTransform[pair.Key];
                long father = pair.Value.Fields.GetReference("m_Father").FileId;

                if (father == 0)
                {
                    Root = node;
                    continue;
                }

                if (nodesByTransform.TryGetValue(father, out Node parent))
                {
                    parent.Children.Add(node);
                }
            }

            OrderChildren(Root, nodesByTransform);
        }

        private void OrderChildren(Node node, Dictionary<long, Node> nodesByTransform)
        {
            if (node == null)
            {
                return;
            }

            List<Node> ordered = new List<Node>();

            foreach (UnityYamlNode childReference in node.Transform.GetOrEmpty("m_Children").Items)
            {
                long childId = childReference.ToReference().FileId;

                if (nodesByTransform.TryGetValue(childId, out Node child))
                {
                    ordered.Add(child);
                }
            }

            foreach (Node child in node.Children)
            {
                if (ordered.Contains(child) == false)
                {
                    ordered.Add(child);
                }
            }

            node.Children.Clear();
            node.Children.AddRange(ordered);

            foreach (Node child in node.Children)
            {
                OrderChildren(child, nodesByTransform);
            }
        }
    }
}
