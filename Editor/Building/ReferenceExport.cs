using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace _Project.Editor.Building
{
    public class ReferenceExport
    {
        public const string RootPath = "C:/Users/Greetrays/Downloads/GoodsSortingTEMP-main/GoodsSortingTEMP-main/ExportedProject/Assets";
        public const string MonoBehaviourFolder = RootPath + "/MonoBehaviour";
        public const string SpriteFolder = RootPath + "/Sprite";
        public const string TextureFolder = RootPath + "/Texture2D";
        public const string ItemPrefabsFolder = RootPath + "/_Games/Prefabs/Buildings/Area_Item";
        public const string BuildingsScenePath = RootPath + "/_Games/Scenes/Buildings/Buildings.unity";
        public const string LocalizationPath = RootPath + "/Resources/I2Languages.asset";

        public static readonly string[] ItemPrefabFolderOrder =
        {
            "Area_Item_1",
            "Area_Item_2",
            "Area_Item_3",
            "Area_Item_4",
            "Area_Item_Halloween"
        };

        private static readonly Regex GuidLine = new Regex(@"^guid: ([0-9a-f]{32})", RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly Dictionary<string, string> _pathByGuid = new Dictionary<string, string>();
        private readonly Dictionary<string, UnityYamlNode> _spriteCache = new Dictionary<string, UnityYamlNode>();
        private readonly UnityYamlParser _parser = new UnityYamlParser();
        private bool _indexed;

        public string FindPathByGuid(string guid)
        {
            EnsureIndexed();
            return _pathByGuid.TryGetValue(guid, out string path) ? path : null;
        }

        public UnityYamlNode LoadSprite(string spriteGuid)
        {
            if (_spriteCache.TryGetValue(spriteGuid, out UnityYamlNode cached))
            {
                return cached;
            }

            string path = FindPathByGuid(spriteGuid);

            if (path == null || path.EndsWith(".asset") == false)
            {
                _spriteCache[spriteGuid] = null;
                return null;
            }

            List<UnityYamlDocument> documents = _parser.ParseFile(path);
            UnityYamlNode sprite = null;

            foreach (UnityYamlDocument document in documents)
            {
                if (document.TypeName == "Sprite")
                {
                    sprite = document.Fields;
                    break;
                }
            }

            _spriteCache[spriteGuid] = sprite;
            return sprite;
        }

        public UnityYamlNode LoadMonoBehaviour(string path)
        {
            foreach (UnityYamlDocument document in _parser.ParseFile(path))
            {
                if (document.TypeName == "MonoBehaviour")
                {
                    return document.Fields;
                }
            }

            throw new InvalidDataException(path);
        }

        public List<UnityYamlDocument> LoadDocuments(string path)
        {
            return _parser.ParseFile(path);
        }

        public string ReadGuid(string assetPath)
        {
            string metaPath = assetPath + ".meta";

            if (File.Exists(metaPath) == false)
            {
                return null;
            }

            Match match = GuidLine.Match(File.ReadAllText(metaPath));
            return match.Success ? match.Groups[1].Value : null;
        }

        private void EnsureIndexed()
        {
            if (_indexed)
            {
                return;
            }

            IndexFolder(SpriteFolder, SearchOption.TopDirectoryOnly);
            IndexFolder(TextureFolder, SearchOption.TopDirectoryOnly);
            IndexFolder(MonoBehaviourFolder, SearchOption.TopDirectoryOnly);
            IndexFolder(ItemPrefabsFolder, SearchOption.AllDirectories);
            _indexed = true;
        }

        private void IndexFolder(string folder, SearchOption option)
        {
            if (Directory.Exists(folder) == false)
            {
                throw new DirectoryNotFoundException(folder);
            }

            foreach (string metaPath in Directory.EnumerateFiles(folder, "*.meta", option))
            {
                string guid = ReadGuidFromMeta(metaPath);

                if (guid == null)
                {
                    continue;
                }

                string assetPath = metaPath.Substring(0, metaPath.Length - ".meta".Length).Replace('\\', '/');
                _pathByGuid[guid] = assetPath;
            }
        }

        private string ReadGuidFromMeta(string metaPath)
        {
            using (StreamReader reader = new StreamReader(metaPath))
            {
                for (int i = 0; i < 4; i++)
                {
                    string line = reader.ReadLine();

                    if (line == null)
                    {
                        return null;
                    }

                    if (line.StartsWith("guid: "))
                    {
                        return line.Substring("guid: ".Length).Trim();
                    }
                }
            }

            return null;
        }
    }
}
