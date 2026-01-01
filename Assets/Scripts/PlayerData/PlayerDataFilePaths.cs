using System.IO;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Centralized file path management for player character data.
    /// Uses PersistentDataPath for player-created content (writable, survives updates).
    /// Falls back to StreamingAssets for default/template characters (read-only).
    /// </summary>
    public static class PlayerDataFilePaths
    {
        /// <summary>
        /// Gets the directory for player-created character files.
        /// Uses PersistentDataPath so files survive game updates and are writable.
        /// </summary>
        public static string GetPlayerCharactersDirectory()
        {
            string path = Path.Combine(Application.persistentDataPath, "Characters");
            
            // Ensure directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }

        /// <summary>
        /// Gets the directory for default/template character files.
        /// Uses StreamingAssets (read-only, bundled with game).
        /// </summary>
        public static string GetTemplateCharactersDirectory()
        {
            return Path.Combine(Application.streamingAssetsPath, "Characters");
        }

        /// <summary>
        /// Gets the full path for a player character file.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="fileName">Name of the file (e.g., "MyCharacter.json")</param>
        /// <returns>Full path to the file</returns>
        public static string GetPlayerCharacterPath(string fileName)
        {
            return Path.Combine(GetPlayerCharactersDirectory(), fileName);
        }

        /// <summary>
        /// Gets the full path for a template character file.
        /// </summary>
        /// <param name="fileName">Name of the file (e.g., "ExampleCharacter.json")</param>
        /// <returns>Full path to the file</returns>
        public static string GetTemplateCharacterPath(string fileName)
        {
            return Path.Combine(GetTemplateCharactersDirectory(), fileName);
        }

        /// <summary>
        /// Tries to find a character file, checking player directory first, then templates.
        /// </summary>
        /// <param name="fileName">Name of the file to find</param>
        /// <returns>Full path if found, null otherwise</returns>
        public static string FindCharacterFile(string fileName)
        {
            // Check player directory first (writable, user's characters)
            string playerPath = GetPlayerCharacterPath(fileName);
            if (File.Exists(playerPath))
            {
                return playerPath;
            }

            // Fall back to templates (read-only, default characters)
            string templatePath = GetTemplateCharacterPath(fileName);
            if (File.Exists(templatePath))
            {
                return templatePath;
            }

            return null;
        }

        /// <summary>
        /// Gets a user-friendly display path for logging/debugging.
        /// </summary>
        public static string GetDisplayPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return "null";

            // Show relative path if in known directories
            if (fullPath.StartsWith(Application.persistentDataPath))
            {
                return "PlayerData/" + fullPath.Substring(Application.persistentDataPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            if (fullPath.StartsWith(Application.streamingAssetsPath))
            {
                return "StreamingAssets/" + fullPath.Substring(Application.streamingAssetsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return fullPath;
        }
    }
}

