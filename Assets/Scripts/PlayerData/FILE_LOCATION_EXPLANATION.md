# File Location for Character Data - Explanation

## Why Not StreamingAssets?

**StreamingAssets** is read-only and gets overwritten when the game updates. Not ideal for player-created content!

## Better Approach: PersistentDataPath

### For Player Characters (Writable)
- **Location**: `Application.persistentDataPath/Characters/`
- **Windows**: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<GameName>\Characters\`
- **Benefits**:
  - ✅ **Writable**: Players can save their characters
  - ✅ **Survives Updates**: Not overwritten when game updates
  - ✅ **User-Specific**: Each user has their own characters
  - ✅ **Industry Standard**: How most games store save data

### For Template Characters (Read-Only)
- **Location**: `StreamingAssets/Characters/`
- **Benefits**:
  - ✅ **Bundled with Game**: Default characters included
  - ✅ **Read-Only**: Can't be accidentally deleted
  - ✅ **Templates**: Examples for players to copy

## How It Works

The `PlayerDataFilePaths` class handles this automatically:

1. **First**: Checks `PersistentDataPath/Characters/` (player's characters)
2. **Fallback**: Checks `StreamingAssets/Characters/` (templates)
3. **New Files**: Saves to `PersistentDataPath/Characters/` (writable)

## Example Paths

### Windows
```
Player Characters:
C:\Users\YourName\AppData\LocalLow\YourCompany\VTT\Characters\MyCharacter.json

Templates:
<GameInstall>\VTT_Data\StreamingAssets\Characters\ExampleCharacter.json
```

### Benefits for Players

1. **Save Characters**: Can save changes to their characters
2. **Multiple Characters**: Can have multiple character files
3. **Backup**: Easy to backup (just copy the Characters folder)
4. **Share**: Can share character files with other players
5. **Survives Updates**: Characters aren't lost when game updates

## Migration

If you have characters in `StreamingAssets/Characters/`:
- They'll still work (read-only)
- New characters will be saved to `PersistentDataPath/Characters/`
- You can copy template characters to the player directory to make them editable

