using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace _Project.Editor.Building
{
    public class UnityYamlParser
    {
        private static readonly Regex DocumentHeader = new Regex(@"^--- !u!(\d+) &(-?\d+)", RegexOptions.Compiled);

        private struct Line
        {
            public int Indent;
            public string Text;
        }

        private List<Line> _lines;
        private int _position;

        public List<UnityYamlDocument> ParseFile(string path)
        {
            return ParseText(File.ReadAllText(path));
        }

        public List<UnityYamlDocument> ParseText(string text)
        {
            List<UnityYamlDocument> documents = new List<UnityYamlDocument>();
            string[] rawLines = text.Split('\n');
            int index = 0;

            while (index < rawLines.Length)
            {
                string raw = rawLines[index].TrimEnd('\r');
                Match header = DocumentHeader.Match(raw);

                if (header.Success == false)
                {
                    index++;
                    continue;
                }

                int classId = int.Parse(header.Groups[1].Value);
                long fileId = long.Parse(header.Groups[2].Value);
                index++;
                int start = index;

                while (index < rawLines.Length && rawLines[index].StartsWith("--- ") == false)
                {
                    index++;
                }

                UnityYamlNode root = ParseLines(rawLines, start, index);
                string typeName = string.Empty;
                UnityYamlNode fields = root;

                if (root.IsMap && root.Map.Count == 1)
                {
                    foreach (KeyValuePair<string, UnityYamlNode> pair in root.Map)
                    {
                        typeName = pair.Key;
                        fields = pair.Value;
                    }
                }

                documents.Add(new UnityYamlDocument(classId, fileId, typeName, fields));
            }

            return documents;
        }

        public UnityYamlNode ParseLines(string[] rawLines, int start, int end)
        {
            _lines = new List<Line>(end - start);

            for (int i = start; i < end; i++)
            {
                string raw = rawLines[i].TrimEnd('\r');

                if (raw.Length == 0 || raw[0] == '%' || raw[0] == '#')
                {
                    continue;
                }

                int indent = 0;

                while (indent < raw.Length && raw[indent] == ' ')
                {
                    indent++;
                }

                string content = raw.Substring(indent);

                if (content.Length == 0)
                {
                    continue;
                }

                _lines.Add(new Line { Indent = indent, Text = content });
            }

            _position = 0;

            if (_lines.Count == 0)
            {
                return new UnityYamlNode(new Dictionary<string, UnityYamlNode>());
            }

            return ParseBlock(_lines[0].Indent);
        }

        private UnityYamlNode ParseBlock(int indent)
        {
            Line line = _lines[_position];

            if (IsSequenceItem(line.Text))
            {
                return ParseSequence(indent);
            }

            if (IsMappingLine(line.Text))
            {
                return ParseMapping(indent);
            }

            _position++;
            return ParseInline(line.Text);
        }

        private UnityYamlNode ParseMapping(int indent)
        {
            Dictionary<string, UnityYamlNode> map = new Dictionary<string, UnityYamlNode>();

            while (_position < _lines.Count)
            {
                Line line = _lines[_position];

                if (line.Indent != indent || IsSequenceItem(line.Text) || IsMappingLine(line.Text) == false)
                {
                    break;
                }

                SplitKeyValue(line.Text, out string key, out string rest);
                _position++;

                UnityYamlNode value;

                if (rest.Length == 0)
                {
                    value = ParseNestedValue(indent);
                }
                else
                {
                    value = ParseInline(rest);
                }

                map[key] = value;
            }

            return new UnityYamlNode(map);
        }

        private UnityYamlNode ParseNestedValue(int parentIndent)
        {
            if (_position >= _lines.Count)
            {
                return new UnityYamlNode(string.Empty);
            }

            Line next = _lines[_position];

            if (next.Indent > parentIndent)
            {
                return ParseBlock(next.Indent);
            }

            if (next.Indent == parentIndent && IsSequenceItem(next.Text))
            {
                return ParseSequence(parentIndent);
            }

            return new UnityYamlNode(string.Empty);
        }

        private UnityYamlNode ParseSequence(int indent)
        {
            List<UnityYamlNode> items = new List<UnityYamlNode>();

            while (_position < _lines.Count)
            {
                Line line = _lines[_position];

                if (line.Indent != indent || IsSequenceItem(line.Text) == false)
                {
                    break;
                }

                string content = line.Text.Length > 1 ? line.Text.Substring(2).TrimStart() : string.Empty;

                if (content.Length == 0)
                {
                    _position++;
                    items.Add(ParseNestedValue(indent));
                    continue;
                }

                if (IsSequenceItem(content))
                {
                    _lines[_position] = new Line { Indent = indent + 2, Text = content };
                    items.Add(ParseSequence(indent + 2));
                    continue;
                }

                if (IsMappingLine(content))
                {
                    _lines[_position] = new Line { Indent = indent + 2, Text = content };
                    items.Add(ParseMapping(indent + 2));
                    continue;
                }

                _position++;
                items.Add(ParseInline(content));
            }

            return new UnityYamlNode(items);
        }

        private UnityYamlNode ParseInline(string text)
        {
            text = text.Trim();

            if (text.Length == 0)
            {
                return new UnityYamlNode(string.Empty);
            }

            if (text[0] == '{')
            {
                return ParseFlowMap(text);
            }

            if (text[0] == '[')
            {
                return ParseFlowSequence(text);
            }

            return new UnityYamlNode(ParseScalar(text));
        }

        private UnityYamlNode ParseFlowMap(string text)
        {
            Dictionary<string, UnityYamlNode> map = new Dictionary<string, UnityYamlNode>();
            string inner = text.Substring(1, text.Length - 2).Trim();

            foreach (string part in SplitTopLevel(inner))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                SplitKeyValue(part, out string key, out string rest);
                map[key] = ParseInline(rest);
            }

            return new UnityYamlNode(map);
        }

        private UnityYamlNode ParseFlowSequence(string text)
        {
            List<UnityYamlNode> items = new List<UnityYamlNode>();
            string inner = text.Substring(1, text.Length - 2).Trim();

            foreach (string part in SplitTopLevel(inner))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                items.Add(ParseInline(part));
            }

            return new UnityYamlNode(items);
        }

        private List<string> SplitTopLevel(string text)
        {
            List<string> parts = new List<string>();
            int depth = 0;
            bool inSingle = false;
            bool inDouble = false;
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inSingle)
                {
                    current.Append(c);

                    if (c == '\'')
                    {
                        inSingle = false;
                    }

                    continue;
                }

                if (inDouble)
                {
                    current.Append(c);

                    if (c == '"' && text[i - 1] != '\\')
                    {
                        inDouble = false;
                    }

                    continue;
                }

                if (c == '\'')
                {
                    inSingle = true;
                }
                else if (c == '"')
                {
                    inDouble = true;
                }
                else if (c == '{' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            parts.Add(current.ToString().Trim());
            return parts;
        }

        private string ParseScalar(string text)
        {
            if (text.Length >= 2 && text[0] == '\'' && text[text.Length - 1] == '\'')
            {
                return text.Substring(1, text.Length - 2).Replace("''", "'");
            }

            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            {
                return Regex.Unescape(text.Substring(1, text.Length - 2));
            }

            return text;
        }

        private void SplitKeyValue(string text, out string key, out string rest)
        {
            int separator = text.IndexOf(": ", StringComparison.Ordinal);

            if (separator < 0)
            {
                if (text.EndsWith(":"))
                {
                    key = text.Substring(0, text.Length - 1).Trim();
                    rest = string.Empty;
                    return;
                }

                key = text.Trim();
                rest = string.Empty;
                return;
            }

            key = text.Substring(0, separator).Trim();
            rest = text.Substring(separator + 2).Trim();
        }

        private bool IsSequenceItem(string text)
        {
            return text == "-" || text.StartsWith("- ");
        }

        private bool IsMappingLine(string text)
        {
            if (text[0] == '{' || text[0] == '[' || text[0] == '\'' || text[0] == '"')
            {
                return false;
            }

            return text.IndexOf(": ", StringComparison.Ordinal) > 0 || (text.EndsWith(":") && text.IndexOf(' ') < 0);
        }
    }
}
