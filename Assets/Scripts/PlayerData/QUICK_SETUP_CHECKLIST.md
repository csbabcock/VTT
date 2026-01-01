# Quick Setup Checklist

## ✅ Minimum Setup (2 Steps)

### 1. Add Component to Scene
- [ ] Open your game scene
- [ ] Create or select a GameObject (e.g., "GameManager")
- [ ] Add Component → `PlayerDataServiceInitializer`

### 2. Configure in Inspector
- [ ] **Data Source**: Set to `JSON`
- [ ] **JSON File Path**: Enter `"Characters/MyCharacter.json"`
- [ ] **Initialize On Awake**: ✅ Checked

**Done!** The system will use default values on first run, then create the file when you save.

---

## 📋 Full Setup (Recommended)

### Step 1: Create JSON File (Optional but Recommended)
- [ ] Create folder: `Assets/StreamingAssets/Characters/`
- [ ] Create file: `MyCharacter.json` in that folder
- [ ] Copy template from `ExampleCharacter.json`
- [ ] Fill in your character's stats

### Step 2: Add Component
- [ ] Add `PlayerDataServiceInitializer` to a GameObject in scene

### Step 3: Configure
- [ ] **Data Source**: `JSON`
- [ ] **JSON File Path**: `"Characters/MyCharacter.json"`
- [ ] **Initialize On Awake**: ✅ Checked

### Step 4: Test
- [ ] Press Play
- [ ] Press Tab to open character sheet
- [ ] Verify character data appears
- [ ] Check Console for "PlayerDataService initialized" message

---

## 🎯 Inspector Settings Reference

When you select the GameObject with `PlayerDataServiceInitializer`, you'll see:

```
PlayerDataServiceInitializer
├── Data Source
│   └── [Dropdown] JSON / ScriptableObject / Default
│
├── JSON Configuration
│   └── JSON File Path: "Characters/MyCharacter.json"
│
├── ScriptableObject Configuration
│   └── Player Data Asset: [None] (only if using ScriptableObject)
│
└── Initialization
    ├── Initialize On Awake: ✅
    └── Initialize On Start: ❌
```

---

## 📁 File Locations

### Where to Put JSON Files:

**Templates (Read-Only)**:
```
Assets/StreamingAssets/Characters/MyCharacter.json
```

**Player Characters (Writable)**:
```
%USERPROFILE%\AppData\LocalLow\<Company>\<Game>\Characters\MyCharacter.json
```
(Windows - created automatically at runtime)

---

## ⚠️ Common Issues

### "File not found" Error
- ✅ Check JSON File Path doesn't have leading slash: `"Characters/File.json"` not `"/Characters/File.json"`
- ✅ Make sure file exists in `StreamingAssets/Characters/` (for templates)
- ✅ First run will use defaults if file doesn't exist (this is OK!)

### UI Not Showing Data
- ✅ Check Console for initialization message
- ✅ Make sure `Initialize On Awake` is checked
- ✅ Verify `InGameUIPresenter` is in the scene

### Proficiency Markers Not Showing
- ✅ Check JSON has `_proficientSkillsStrings` array
- ✅ Use exact skill names: "Athletics", "Intimidation", etc.

---

## 🚀 That's It!

Once configured, the system will:
1. ✅ Load character data on scene start
2. ✅ Update UI automatically
3. ✅ Save changes to player directory
4. ✅ Work with both templates and player files

