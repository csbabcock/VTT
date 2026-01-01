using System.IO;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Service for loading player character data from JSON files.
    /// Supports both Unity's JsonUtility and System.Text.Json (if available).
    /// </summary>
    public static class PlayerDataJsonLoader
    {
        /// <summary>
        /// Loads character data from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON file (relative to StreamingAssets or absolute path).</param>
        /// <returns>Loaded character data, or null if loading failed.</returns>
        public static DnD5eCharacterData LoadFromFile(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"PlayerDataJsonLoader: File not found at {fullPath}");
                    return null;
                }

                string jsonContent = File.ReadAllText(fullPath);
                return LoadFromJson(jsonContent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerDataJsonLoader: Error loading file {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads character data from a JSON string.
        /// </summary>
        /// <param name="jsonContent">JSON string containing character data.</param>
        /// <returns>Loaded character data, or null if parsing failed.</returns>
        public static DnD5eCharacterData LoadFromJson(string jsonContent)
        {
            try
            {
                // Use Unity's JsonUtility (works with Serializable classes)
                DnD5eCharacterData data = JsonUtility.FromJson<DnD5eCharacterData>(jsonContent);
                
                if (data == null)
                {
                    Debug.LogError("PlayerDataJsonLoader: Failed to parse JSON content.");
                    return null;
                }

                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerDataJsonLoader: Error parsing JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves character data to a JSON file.
        /// </summary>
        /// <param name="data">Character data to save.</param>
        /// <param name="filePath">Path to save the file (relative to StreamingAssets or absolute path).</param>
        /// <returns>True if save was successful, false otherwise.</returns>
        public static bool SaveToFile(DnD5eCharacterData data, string filePath)
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(data, prettyPrint: true);
                string fullPath = GetFullPath(filePath);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fullPath, jsonContent);
                Debug.Log($"PlayerDataJsonLoader: Saved character data to {fullPath}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerDataJsonLoader: Error saving file {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts character data to JSON string.
        /// </summary>
        /// <param name="data">Character data to convert.</param>
        /// <param name="prettyPrint">Whether to format the JSON with indentation.</param>
        /// <returns>JSON string representation of the character data.</returns>
        public static string ToJson(DnD5eCharacterData data, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(data, prettyPrint);
        }

        /// <summary>
        /// Gets the full path for a file, checking player directory first, then templates.
        /// </summary>
        private static string GetFullPath(string filePath)
        {
            // If it's already an absolute path, use it
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            // Extract just the filename if a path was provided
            string fileName = Path.GetFileName(filePath);
            
            // Try to find the file (checks player directory first, then templates)
            string foundPath = PlayerDataFilePaths.FindCharacterFile(fileName);
            if (foundPath != null)
            {
                return foundPath;
            }

            // If not found, assume it's a new file and use player directory
            return PlayerDataFilePaths.GetPlayerCharacterPath(fileName);
        }
    }
}

