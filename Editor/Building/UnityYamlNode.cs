using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class UnityYamlNode
    {
        private static readonly UnityYamlNode EmptyNode = new UnityYamlNode(string.Empty);

        public UnityYamlNode(string scalar)
        {
            Scalar = scalar;
        }

        public UnityYamlNode(Dictionary<string, UnityYamlNode> map)
        {
            Map = map;
        }

        public UnityYamlNode(List<UnityYamlNode> sequence)
        {
            Sequence = sequence;
        }

        public string Scalar { get; }
        public Dictionary<string, UnityYamlNode> Map { get; }
        public List<UnityYamlNode> Sequence { get; }

        public bool IsScalar => Scalar != null;
        public bool IsMap => Map != null;
        public bool IsSequence => Sequence != null;

        public IReadOnlyList<UnityYamlNode> Items => Sequence ?? (IReadOnlyList<UnityYamlNode>)System.Array.Empty<UnityYamlNode>();

        public UnityYamlNode this[string key]
        {
            get
            {
                if (Map != null && Map.TryGetValue(key, out UnityYamlNode value))
                {
                    return value;
                }

                return null;
            }
        }

        public bool Has(string key)
        {
            return Map != null && Map.ContainsKey(key);
        }

        public string GetString(string key, string fallback = "")
        {
            UnityYamlNode node = this[key];
            return node != null && node.IsScalar ? node.Scalar : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            UnityYamlNode node = this[key];
            return node != null && node.IsScalar ? ParseFloat(node.Scalar, fallback) : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsScalar == false)
            {
                return fallback;
            }

            return int.TryParse(node.Scalar, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : fallback;
        }

        public long GetLong(string key, long fallback = 0)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsScalar == false)
            {
                return fallback;
            }

            return long.TryParse(node.Scalar, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
                ? value
                : fallback;
        }

        public Vector2 GetVector2(string key, Vector2 fallback = default)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return fallback;
            }

            return new Vector2(node.GetFloat("x", fallback.x), node.GetFloat("y", fallback.y));
        }

        public Vector3 GetVector3(string key, Vector3 fallback = default)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return fallback;
            }

            return new Vector3(node.GetFloat("x", fallback.x), node.GetFloat("y", fallback.y), node.GetFloat("z", fallback.z));
        }

        public Vector3Int GetVector3Int(string key)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return Vector3Int.zero;
            }

            return new Vector3Int(node.GetInt("x"), node.GetInt("y"), node.GetInt("z"));
        }

        public Color GetColor(string key, Color fallback)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return fallback;
            }

            return new Color(node.GetFloat("r", fallback.r), node.GetFloat("g", fallback.g), node.GetFloat("b", fallback.b), node.GetFloat("a", fallback.a));
        }

        public Rect GetRect(string key)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return Rect.zero;
            }

            return new Rect(node.GetFloat("x"), node.GetFloat("y"), node.GetFloat("width"), node.GetFloat("height"));
        }

        public UnityYamlReference GetReference(string key)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return UnityYamlReference.Null;
            }

            return node.ToReference();
        }

        public UnityYamlReference ToReference()
        {
            if (IsMap == false)
            {
                return UnityYamlReference.Null;
            }

            return new UnityYamlReference(GetLong("fileID"), GetString("guid", null), GetInt("type"));
        }

        public Matrix4x4 GetMatrix(string key)
        {
            UnityYamlNode node = this[key];

            if (node == null || node.IsMap == false)
            {
                return Matrix4x4.identity;
            }

            return node.ToMatrix();
        }

        public Matrix4x4 ToMatrix()
        {
            Matrix4x4 matrix = Matrix4x4.identity;

            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    matrix[row, column] = GetFloat("e" + row + column, matrix[row, column]);
                }
            }

            return matrix;
        }

        public UnityYamlNode GetOrEmpty(string key)
        {
            return this[key] ?? EmptyNode;
        }

        private float ParseFloat(string text, float fallback)
        {
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }

            if (text == "-0")
            {
                return 0f;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : fallback;
        }
    }
}
