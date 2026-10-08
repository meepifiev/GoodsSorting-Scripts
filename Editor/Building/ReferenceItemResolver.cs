using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class ReferenceItemResolver
    {
        private static readonly Regex NameWithSuffix = new Regex(@"^(.*?)(?: (\d+))?$", RegexOptions.Compiled);
        private static readonly string[] PrefabSubfolders = { "Item", "Item_Broken" };

        public class ResolvedItem
        {
            public int Id;
            public string SoName;
            public string PrefabPath;
            public Vector3 IsoSize;
        }

        private readonly ReferenceExport _export;
        private readonly BuildingImportReport _report;
        private List<string> _soGuids;

        public ReferenceItemResolver(ReferenceExport export, BuildingImportReport report)
        {
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public ResolvedItem Resolve(int itemId)
        {
            EnsureList();

            if (itemId < 0 || itemId >= _soGuids.Count)
            {
                _report.AddAmbiguousItem("id " + itemId + ": outside SOBuildingItemAddressable list (" + _soGuids.Count + ")");
                return null;
            }

            string soPath = _export.FindPathByGuid(_soGuids[itemId]);

            if (soPath == null)
            {
                _report.AddAmbiguousItem("id " + itemId + ": BuildingItem SO not found for guid " + _soGuids[itemId]);
                return null;
            }

            UnityYamlNode so = _export.LoadMonoBehaviour(soPath);
            string soName = so.GetString("m_Name", Path.GetFileNameWithoutExtension(soPath));
            Vector3 size = so.GetVector3("originalSize", Vector3.one);

            Match match = NameWithSuffix.Match(soName);
            string baseName = match.Groups[1].Value;
            int folderOrdinal = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;

            List<List<string>> candidatesByFolder = FindCandidates(baseName);

            if (candidatesByFolder.Count == 0)
            {
                _report.AddAmbiguousItem("id " + itemId + " (" + soName + "): no prefab named " + baseName);
                return null;
            }

            if (folderOrdinal >= candidatesByFolder.Count)
            {
                _report.AddAmbiguousItem("id " + itemId + " (" + soName + "): suffix " + folderOrdinal + " but only " + candidatesByFolder.Count + " folders contain " + baseName + "; using last");
                folderOrdinal = candidatesByFolder.Count - 1;
            }

            List<string> candidates = candidatesByFolder[folderOrdinal];

            if (candidates.Count > 1)
            {
                _report.AddAmbiguousItem("id " + itemId + " (" + soName + "): " + candidates.Count + " prefabs in one folder: " + string.Join(", ", candidates) + "; using first");
            }

            return new ResolvedItem
            {
                Id = itemId,
                SoName = soName,
                PrefabPath = candidates[0],
                IsoSize = size
            };
        }

        private List<List<string>> FindCandidates(string baseName)
        {
            List<List<string>> result = new List<List<string>>();

            foreach (string folder in ReferenceExport.ItemPrefabFolderOrder)
            {
                List<string> hits = new List<string>();

                foreach (string subfolder in PrefabSubfolders)
                {
                    string path = ReferenceExport.ItemPrefabsFolder + "/" + folder + "/" + subfolder + "/" + baseName + ".prefab";

                    if (File.Exists(path))
                    {
                        hits.Add(path);
                    }
                }

                if (hits.Count > 0)
                {
                    result.Add(hits);
                }
            }

            return result;
        }

        private void EnsureList()
        {
            if (_soGuids != null)
            {
                return;
            }

            _soGuids = new List<string>();
            UnityYamlNode list = _export.LoadMonoBehaviour(ReferenceExport.MonoBehaviourFolder + "/SOBuildingItemAddressable.asset");

            foreach (UnityYamlNode item in list.GetOrEmpty("items").Items)
            {
                UnityYamlReference reference = item.ToReference();
                _soGuids.Add(reference.IsExternal ? reference.Guid : null);
            }
        }
    }
}
