using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Service for enumerating and managing character files.
    /// Provides methods to list available characters from both player and template directories.
    /// </summary>
    public static class CharacterFileService
    {
        /// <summary>
        /// Represents a character file with its metadata.
        /// </summary>
        public class CharacterFileInfo
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }
            public bool IsPlayerCreated { get; set; }
            public DnD5eCharacterData CharacterData { get; set; }
        }

        /// <summary>
        /// Gets all available character files, combining player-created and template characters.
        /// Player-created characters take precedence if a file with the same name exists in both locations.
        /// </summary>
        /// <returns>List of character file information</returns>
        public static List<CharacterFileInfo> GetAllCharacterFiles()
        {
            var characterFiles = new Dictionary<string, CharacterFileInfo>();

            // First, load template characters (read-only, bundled with game)
            string templateDir = PlayerDataFilePaths.GetTemplateCharactersDirectory();
            if (Directory.Exists(templateDir))
            {
                LoadCharactersFromDirectory(templateDir, characterFiles, isPlayerCreated: false);
            }

            // Then, load player-created characters (writable, takes precedence)
            string playerDir = PlayerDataFilePaths.GetPlayerCharactersDirectory();
            if (Directory.Exists(playerDir))
            {
                LoadCharactersFromDirectory(playerDir, characterFiles, isPlayerCreated: true);
            }

            return characterFiles.Values.OrderBy(c => c.CharacterData?.characterName ?? c.FileName).ToList();
        }

        /// <summary>
        /// Loads character files from a specific directory.
        /// </summary>
        private static void LoadCharactersFromDirectory(string directory, Dictionary<string, CharacterFileInfo> characterFiles, bool isPlayerCreated)
        {
            try
            {
                string[] jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);

                foreach (string filePath in jsonFiles)
                {
                    string fileName = Path.GetFileName(filePath);

                    // Skip if we already have a player-created version of this file
                    if (characterFiles.ContainsKey(fileName) && characterFiles[fileName].IsPlayerCreated)
                    {
                        continue;
                    }

                    // Try to load the character data (use absolute path directly)
                    DnD5eCharacterData characterData = null;
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            string jsonContent = File.ReadAllText(filePath);
                            characterData = PlayerDataJsonLoader.LoadFromJson(jsonContent);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"CharacterFileService: Error loading {filePath}: {ex.Message}");
                    }

                    if (characterData != null)
                    {
                        characterFiles[fileName] = new CharacterFileInfo
                        {
                            FileName = fileName,
                            FullPath = filePath,
                            IsPlayerCreated = isPlayerCreated,
                            CharacterData = characterData
                        };
                    }
                    else
                    {
                        Debug.LogWarning($"CharacterFileService: Failed to load character from {filePath}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CharacterFileService: Error loading characters from {directory}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a character file by its filename.
        /// </summary>
        /// <param name="fileName">The filename (e.g., "MyCharacter.json")</param>
        /// <returns>Character file info if found, null otherwise</returns>
        public static CharacterFileInfo GetCharacterFile(string fileName)
        {
            var allFiles = GetAllCharacterFiles();
            return allFiles.FirstOrDefault(f => f.FileName == fileName);
        }

        /// <summary>
        /// Gets the display name for a character (for UI purposes).
        /// Shows: "CharacterName - Level X Class - Race"
        /// </summary>
        public static string GetCharacterDisplayName(CharacterFileInfo fileInfo)
        {
            if (fileInfo?.CharacterData == null)
            {
                return Path.GetFileNameWithoutExtension(fileInfo?.FileName ?? "Unknown");
            }

            var data = fileInfo.CharacterData;
            string displayName = data.characterName;

            if (data.level > 0)
            {
                displayName += $" - Level {data.level}";
            }

            if (!string.IsNullOrEmpty(data.characterClass))
            {
                displayName += $" {data.characterClass}";
            }

            if (!string.IsNullOrEmpty(data.race))
            {
                displayName += $" - {data.race}";
            }

            return displayName;
        }

        /// <summary>
        /// Gets a short description for a character card (for UI purposes).
        /// Shows: "Level X Class - Race"
        /// </summary>
        public static string GetCharacterCardSubtitle(CharacterFileInfo fileInfo)
        {
            if (fileInfo?.CharacterData == null)
            {
                return "Click to select";
            }

            var data = fileInfo.CharacterData;
            string subtitle = "";

            if (data.level > 0)
            {
                subtitle = $"Level {data.level}";
            }

            if (!string.IsNullOrEmpty(data.characterClass))
            {
                if (!string.IsNullOrEmpty(subtitle))
                    subtitle += " ";
                subtitle += data.characterClass;
            }

            if (!string.IsNullOrEmpty(data.race))
            {
                if (!string.IsNullOrEmpty(subtitle))
                    subtitle += " - ";
                subtitle += data.race;
            }

            return string.IsNullOrEmpty(subtitle) ? "Click to select" : subtitle;
        }
    }
}

