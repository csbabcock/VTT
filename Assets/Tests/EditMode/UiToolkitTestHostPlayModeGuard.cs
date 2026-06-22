#if UNITY_EDITOR
using UnityEditor;

namespace GameCore.Tests.EditMode
{
    [InitializeOnLoad]
    static class UiToolkitTestHostPlayModeGuard
    {
        static UiToolkitTestHostPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                UiToolkitTestHost.DestroyAllStrays();
        }
    }
}
#endif
