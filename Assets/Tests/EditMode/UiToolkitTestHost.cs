#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Attaches UI Toolkit elements to a panel in Edit Mode without creating
    /// <see cref="EditorWindow"/> instances (those can corrupt MPPM play-mode layout).
    /// </summary>
    internal sealed class UiToolkitTestHost : System.IDisposable
    {
        private const string HostObjectName = "UIToolkitTestHost";
        private const string LegacyWindowTitle = "VTT UI Test Host";

        private GameObject _hostObject;
        private PanelSettings _panelSettings;

        public void Attach(VisualElement root)
        {
            Dispose();

            _hostObject = new GameObject(HostObjectName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var document = _hostObject.AddComponent<UIDocument>();
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.hideFlags = HideFlags.HideAndDontSave;
            document.panelSettings = _panelSettings;
            document.rootVisualElement.Add(root);
        }

        public void Dispose()
        {
            if (_panelSettings != null)
            {
                Object.DestroyImmediate(_panelSettings);
                _panelSettings = null;
            }

            if (_hostObject != null)
            {
                Object.DestroyImmediate(_hostObject);
                _hostObject = null;
            }
        }

        public static void DestroyAllStrays()
        {
            DestroyStrayHostDocuments();
            CloseLegacyEditorWindows();
        }

        private static void DestroyStrayHostDocuments()
        {
            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include);

            for (int i = 0; i < documents.Length; i++)
            {
                var doc = documents[i];
                if (doc != null && doc.gameObject.name == HostObjectName)
                    Object.DestroyImmediate(doc.gameObject);
            }
        }

        private static void CloseLegacyEditorWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                var window = windows[i];
                if (window == null)
                    continue;

                string title = window.titleContent != null ? window.titleContent.text : null;
                if (title != LegacyWindowTitle)
                    continue;

                window.Close();
                Object.DestroyImmediate(window);
            }
        }
    }
}
#endif
