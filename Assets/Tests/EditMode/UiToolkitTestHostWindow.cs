#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Hidden utility window that gives UI Toolkit elements a panel in Edit Mode tests
    /// without being saved into the editor layout (which causes play-mode layout errors).
    /// </summary>
    internal sealed class UiToolkitTestHostWindow : EditorWindow
    {
        static UiToolkitTestHostWindow()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static UiToolkitTestHostWindow Open(VisualElement root)
        {
            var window = CreateInstance<UiToolkitTestHostWindow>();
            window.titleContent = new GUIContent("VTT UI Test Host");
            window.rootVisualElement.Add(root);
            window.ShowUtility();
            MarkDontSaveToLayout(window);
            return window;
        }

        public static void CloseIfOpen(UiToolkitTestHostWindow window)
        {
            if (window == null)
                return;

            window.Close();
            DestroyImmediate(window);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var windows = Resources.FindObjectsOfTypeAll<UiToolkitTestHostWindow>();
            foreach (var window in windows)
                CloseIfOpen(window);
        }

        private static void MarkDontSaveToLayout(EditorWindow window)
        {
            var editorAssembly = typeof(EditorWindow).Assembly;
            var hostViewType = editorAssembly.GetType("UnityEditor.HostView");
            var containerWindowType = editorAssembly.GetType("UnityEditor.ContainerWindow");
            if (hostViewType == null || containerWindowType == null)
                return;

            var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
            var parentView = parentField?.GetValue(window);
            if (parentView == null)
                return;

            var containerWindowProperty = hostViewType.GetProperty("window", BindingFlags.Instance | BindingFlags.Public);
            var containerWindow = containerWindowProperty?.GetValue(parentView);
            if (containerWindow == null)
                return;

            var dontSaveField = containerWindowType.GetField(
                "m_DontSaveToLayout",
                BindingFlags.Instance | BindingFlags.NonPublic);
            dontSaveField?.SetValue(containerWindow, true);
        }
    }
}
#endif
