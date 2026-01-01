# Unity Editor Setup Guide

## Quick Setup Steps

### Step 1: Add PlayerDataServiceInitializer to Scene

1. **Open your game scene** (the one where the character sheet UI is used)

2. **Create or select a GameObject** to hold the initializer:
   - Option A: Create a new empty GameObject named "PlayerDataManager"
   - Option B: Use an existing manager GameObject (like "GameManager" or "UIManager")

3. **Add the Component**:
   - Select the GameObject
   - In Inspector, click "Add Component"
   - Search for "PlayerDataServiceInitializer"
   - Add it

### Step 2: Configure the Initializer

In the Inspector, you'll see the `PlayerDataServiceInitializer` component with these fields:

#### Data Source
- **Dropdown**: Choose one of:
  - **JSON** (Recommended for player characters)
  - **ScriptableObject** (For designer-created templates)
  - **Default** (Uses hardcoded defaults)

#### JSON Configuration (if Data Source = JSON)
- **JSON File Path**: Enter the path to your JSON file
  - Example: `"Characters/MyCharacter.json"`
  - Or just: `"MyCharacter.json"`
  - The system will look in:
    1. `PersistentDataPath/Characters/` (player's save folder - writable)
    2. `StreamingAssets/Characters/` (templates - read-only)

#### ScriptableObject Configuration (if Data Source = ScriptableObject)
- **Player Data Asset**: Drag a `PlayerDataAsset` ScriptableObject here
  - Create one: Right-click in Project → `Create > Game > Player Data`

#### Initialization
- **Initialize On Awake**: ✅ Checked (recommended)
- **Initialize On Start**: ❌ Unchecked (unless you need it)

### Step 3: Create Your Character JSON File

#### Option A: Create in StreamingAssets (Template/Default)

1. **Create folder structure**:
   - `Assets/StreamingAssets/Characters/`
   - (Unity will create StreamingAssets if it doesn't exist)

2. **Create JSON file**:
   - Right-click in `Characters` folder
   - Create → Text Asset (or just create a `.json` file)
   - Name it: `MyCharacter.json` (or any name)

3. **Edit the JSON**:
   - Double-click to open in your text editor
   - Copy the template from `ExampleCharacter.json` or `JSON_SETUP_GUIDE.md`
   - Fill in your character's stats

4. **Set the path in Initializer**:
   - JSON File Path: `"Characters/MyCharacter.json"`

#### Option B: Let System Create It (Recommended)

1. **Just set the path**:
   - JSON File Path: `"Characters/MyCharacter.json"`
   - The system will create the file in `PersistentDataPath/Characters/` when you save

2. **First run will use defaults**, then you can edit the file

### Step 4: Verify Setup

1. **Check the GameObject**:
   - ✅ `PlayerDataServiceInitializer` component is attached
   - ✅ Data Source is set correctly
   - ✅ JSON File Path is set (if using JSON)

2. **Check the Scene**:
   - ✅ `InGameUIPresenter` is in the scene (should already be there)
   - ✅ `InGameUIView` is in the scene (should already be there)

3. **Run the Scene**:
   - Press Play
   - Open character sheet (Tab key)
   - Check Console for any errors
   - Verify character data appears in UI

## Complete Example Setup

### Scene Hierarchy
```
Scene
├── GameManager (or PlayerDataManager)
│   └── PlayerDataServiceInitializer (Component)
│       ├── Data Source: JSON
│       ├── JSON File Path: "Characters/MyCharacter.json"
│       ├── Initialize On Awake: ✅
│       └── Initialize On Start: ❌
├── InGameUI (GameObject with InGameUIPresenter)
│   └── InGameUIView (Component)
└── ... (other game objects)
```

### File Structure
```
Assets/
├── StreamingAssets/
│   └── Characters/
│       └── ExampleCharacter.json (template)
└── Scripts/
    └── PlayerData/
        └── (all the scripts)

PersistentDataPath/ (created at runtime)
└── Characters/
    └── MyCharacter.json (player's character - created when saved)
```

## Troubleshooting

### Character Data Not Loading?

1. **Check Console**:
   - Look for errors about file not found
   - Check if path is correct

2. **Verify File Path**:
   - JSON File Path should be: `"Characters/FileName.json"` (no leading slash)
   - Or just: `"FileName.json"`

3. **Check File Location**:
   - For templates: `Assets/StreamingAssets/Characters/`
   - For player files: `%USERPROFILE%\AppData\LocalLow\<Company>\<Game>\Characters\` (Windows)

4. **Validate JSON**:
   - Make sure JSON is valid (no syntax errors)
   - Use an online JSON validator if needed

### UI Not Updating?

1. **Check Initialization Order**:
   - Make sure `PlayerDataServiceInitializer` runs before `InGameUIPresenter`
   - Use "Initialize On Awake" (runs first)

2. **Check Console**:
   - Look for "PlayerDataService initialized" message
   - Check for any errors

3. **Verify Component References**:
   - Make sure `InGameUIPresenter` has `InGameUIView` assigned

### Proficiency Markers Not Showing?

1. **Check JSON**:
   - Make sure `_proficientSkillsStrings` array has correct skill names
   - Use exact names: "Athletics", "Intimidation", etc. (see `DnD5eSkill.cs`)

2. **Check UI**:
   - Open character sheet and check if skills show proficiency icons
   - Verify skill names match exactly

## Quick Reference

### Minimum Setup (Fastest)
1. Add `PlayerDataServiceInitializer` to any GameObject
2. Set Data Source: **JSON**
3. Set JSON File Path: `"Characters/MyCharacter.json"`
4. ✅ Done! (Uses defaults on first run, creates file when saved)

### Full Setup (Recommended)
1. Create `Assets/StreamingAssets/Characters/` folder
2. Create `MyCharacter.json` with your character data
3. Add `PlayerDataServiceInitializer` to GameObject
4. Set Data Source: **JSON**
5. Set JSON File Path: `"Characters/MyCharacter.json"`
6. ✅ Done!

## Notes

- **First Run**: If file doesn't exist, system uses default character data
- **File Location**: Player characters save to `PersistentDataPath` (writable, survives updates)
- **Templates**: Can put example characters in `StreamingAssets/Characters/` (read-only)
- **Multiple Characters**: Create multiple JSON files, change path in Initializer to switch

