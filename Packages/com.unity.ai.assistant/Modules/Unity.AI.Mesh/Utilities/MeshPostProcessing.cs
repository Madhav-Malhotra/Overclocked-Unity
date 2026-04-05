using System;
using Unity.AI.Mesh.Services.Stores.States;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Mesh.Services.Utilities
{
    static class MeshPostProcessing
    {
        /// <summary>
        /// Post-process the specified prefab using the settings specified in `MeshSettingsState`.
        /// The prefab must be already imported into `Assets`.
        /// </summary>
        public static void PostProcessMeshPrefab(GameObject prefab, MeshSettingsState settings)
        {
            if (prefab == null || settings == null ||
                PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
                return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                bool dirty = false;
                dirty |= ApplyPivotMode(prefabRoot, settings.pivotMode);
                
                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// Shifts all direct children of the root so its origin aligns with the desired pivot point.
        /// Returns whether the prefab was modified.
        /// </summary>
        static bool ApplyPivotMode(GameObject prefabRoot, MeshPivotMode pivotMode)
        {
            Bounds? bounds = GetCombinedBounds(prefabRoot);
            if (bounds == null)
                return false;

            Vector3 offset = GetPivotOffset(bounds.Value, pivotMode);
            if (offset.sqrMagnitude < float.Epsilon)
                return false;

            // Move all direct children so the root's origin sits at the target pivot point.
            // Generated meshes do not have mesh components directly on their root node - displacing vertices is not needed.
            foreach (Transform child in prefabRoot.transform)
                child.localPosition -= offset;
            return true;
        }

        static Bounds? GetCombinedBounds(GameObject root)
        {
            Bounds? combined = null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (combined == null)
                    combined = renderer.bounds;
                else
                {
                    Bounds b = combined.Value;
                    b.Encapsulate(renderer.bounds);
                    combined = b;
                }
            }

            return combined;
        }

        static Vector3 GetPivotOffset(Bounds bounds, MeshPivotMode pivotMode)
        {
            return pivotMode switch
            {
                MeshPivotMode.Center => bounds.center,
                MeshPivotMode.BottomCenter => new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                _ => throw new ArgumentOutOfRangeException(nameof(pivotMode), pivotMode, null)
            };
        }
    }
}