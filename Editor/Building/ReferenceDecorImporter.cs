using System;
using _Project.Features.Building;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class ReferenceDecorImporter
    {
        private const string DecorRootName = "Decor";
        private const string SpriteMaterialName = "Sprites-Default.mat";

        private readonly ReferenceSpriteImporter _sprites;
        private readonly BuildingImportReport _report;

        public ReferenceDecorImporter(ReferenceSpriteImporter sprites, BuildingImportReport report)
        {
            _sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Import(ReferenceEnvironment environment, BuildingWorldView world)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (world == null || world.EnvironmentGrid == null)
            {
                throw new InvalidOperationException("Building scene has no environment grid");
            }

            Transform environmentRoot = world.EnvironmentGrid.transform;
            Transform existing = environmentRoot.Find(DecorRootName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            foreach (ReferenceEnvironment.DecorSprite decor in environment.Decor)
            {
                _sprites.Request(decor.SpriteGuid, BuildingAssetPaths.DecorSpritesFolder, false);
            }

            _sprites.ImportAll();

            GameObject root = new GameObject(DecorRootName);
            root.transform.SetParent(environmentRoot, false);
            Material material = AssetDatabase.GetBuiltinExtraResource<Material>(SpriteMaterialName);
            int created = 0;

            foreach (ReferenceEnvironment.DecorSprite decor in environment.Decor)
            {
                Sprite sprite = _sprites.Resolve(decor.SpriteGuid);

                if (sprite == null)
                {
                    _report.Warn("Decor sprite missing: " + decor.Name);
                    continue;
                }

                GameObject decorObject = new GameObject(decor.Name);
                decorObject.transform.SetParent(root.transform, false);
                decorObject.transform.position = decor.WorldPosition;
                decorObject.transform.localScale = decor.WorldScale;

                SpriteRenderer renderer = decorObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = decor.SortingOrder;
                renderer.color = decor.Color;
                renderer.flipX = decor.FlipX;
                renderer.flipY = decor.FlipY;
                created++;
            }

            _report.Count("street decor sprites", created);
        }
    }
}
