#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.EditorTools
{
    /// <summary>
    /// Replaces legacy <see cref="UIDocument"/> with <see cref="PanelRenderer"/> on selected GameObjects (Unity 6+).
    /// Run after scripts require <see cref="PanelRenderer"/> so scenes and prefabs stay valid.
    /// </summary>
    public static class UIDocumentToPanelRendererMigrator
    {
        private const string MenuPath = "Tools/VTT/Migrate UIDocument → Panel Renderer (Selection)";

        [MenuItem(MenuPath, priority = 500)]
        public static void MigrateSelection()
        {
            var objects = Selection.gameObjects;
            if (objects == null || objects.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Migrate UI",
                    "Select one or more GameObjects in the Hierarchy (or open a prefab and select its root).",
                    "OK");
                return;
            }

            int migrated = 0;
            foreach (var root in objects)
            {
                if (root == null)
                    continue;
                foreach (var doc in root.GetComponentsInChildren<UIDocument>(true))
                {
                    if (doc != null)
                    {
                        MigrateOne(doc);
                        migrated++;
                    }
                }
            }

            Debug.Log($"VTT UI migration: replaced {migrated} UIDocument component(s) with PanelRenderer.");
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ValidateMigrateSelection()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        private static void MigrateOne(UIDocument doc)
        {
            var go = doc.gameObject;
            Undo.RegisterCompleteObjectUndo(go, "Migrate UIDocument to PanelRenderer");

            PanelSettings settings = doc.panelSettings;
            VisualTreeAsset vta = doc.visualTreeAsset;
            int sort = Mathf.RoundToInt(doc.sortingOrder);

            // Views use [RequireComponent(typeof(PanelRenderer))], so the object may already
            // have PanelRenderer while UIDocument is still present from the old setup.
            PanelRenderer panel = go.GetComponent<PanelRenderer>();
            if (panel == null)
            {
                Undo.DestroyObjectImmediate(doc);
                panel = Undo.AddComponent<PanelRenderer>(go);
            }
            else
            {
                Undo.RecordObject(panel, "Migrate UIDocument to PanelRenderer");
                Undo.DestroyObjectImmediate(doc);
            }

            Undo.RecordObject(panel, "Migrate UIDocument to PanelRenderer");
            panel.panelSettings = settings;
            panel.visualTreeAsset = vta;
            panel.sortingOrder = sort;

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(go);
        }
    }
}
#endif
