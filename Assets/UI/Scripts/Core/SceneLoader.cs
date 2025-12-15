using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.UI
{
    /// <summary>
    /// Centralized helper for loading scenes.
    /// Keeps scene loading logic out of presenters for easier testing.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// Load a scene synchronously.
        /// </summary>
        public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader.LoadScene called with null or empty scene name.");
                return;
            }

            try
            {
                SceneManager.LoadScene(sceneName, mode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load scene '{sceneName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Load a scene asynchronously.
        /// Returns the AsyncOperation so callers can track progress.
        /// </summary>
        public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader.LoadSceneAsync called with null or empty scene name.");
                return null;
            }

            try
            {
                return SceneManager.LoadSceneAsync(sceneName, mode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load scene asynchronously '{sceneName}': {ex.Message}");
                return null;
            }
        }
    }
}

