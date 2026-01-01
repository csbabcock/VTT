# JSON Character Setup Guide

## Quick Start: Setting Up a Character with JSON

### Step 1: Create a JSON Character File

1. **Navigate to** `Assets/StreamingAssets/Characters/` folder
   - If it doesn't exist, create it: `Assets/StreamingAssets/Characters/`

2. **Create a new file** named `MyCharacter.json` (or any name you want)

3. **Copy this template** and fill in your character's details:

```json
{
    "characterName": "Arlen",
    "level": 3,
    "characterClass": "Fighter",
    "subclass": "Champion",
    "race": "Human",
    "subrace": "",
    "background": "Soldier",
    "alignment": "Lawful Good",
    "playerName": "",
    "experiencePoints": 900,
    "strength": 16,
    "dexterity": 14,
    "constitution": 14,
    "intelligence": 10,
    "wisdom": 12,
    "charisma": 8,
    "maxHitPoints": 28,
    "currentHitPoints": 28,
    "temporaryHitPoints": 0,
    "armorClass": 16,
    "initiative": 2,
    "walkingSpeed": 30,
    "flyingSpeed": 0,
    "swimmingSpeed": 0,
    "climbingSpeed": 0,
    "hitDice": "3d10",
    "hitDiceUsed": 0,
    "proficientSavingThrows": [
        "STR",
        "CON"
    ],
    "_proficientSkillsStrings": [
        "Athletics",
        "Intimidation",
        "Perception",
        "Survival"
    ],
    "proficientArmor": [
        "Light",
        "Medium",
        "Heavy",
        "Shields"
    ],
    "proficientWeapons": [
        "Simple",
        "Martial"
    ],
    "proficientTools": [],
    "languages": [
        "Common",
        "Orcish"
    ],
    "hasInspiration": false,
    "exhaustionLevel": 0,
    "conditions": [],
    "deathSaveSuccesses": 0,
    "deathSaveFailures": 0
}
```

### Step 2: Update the Service Initializer

The `PlayerDataServiceInitializer` component needs to be updated to load from JSON instead of ScriptableObject.

**In Unity:**
1. Find the GameObject with `PlayerDataServiceInitializer` component
2. Set the **JSON File Path** field (e.g., `"Characters/MyCharacter.json"`)
3. The service will load from JSON on scene start

### Step 3: Test It

1. **Run the scene**
2. **Open the character sheet** (press Tab)
3. **Check that your character data appears**

## Field Reference

### Basic Information
- `characterName`: Character's name
- `level`: Character level (1-20)
- `characterClass`: Class name (e.g., "Fighter", "Wizard")
- `subclass`: Subclass name (e.g., "Champion", "Evocation")
- `race`: Race name (e.g., "Human", "Elf")
- `subrace`: Subrace name (e.g., "High Elf")
- `background`: Background name (e.g., "Soldier", "Acolyte")
- `alignment`: Alignment (e.g., "Lawful Good")
- `playerName`: Player's name (optional)
- `experiencePoints`: Current XP

### Ability Scores
- `strength`: Strength score (1-30)
- `dexterity`: Dexterity score (1-30)
- `constitution`: Constitution score (1-30)
- `intelligence`: Intelligence score (1-30)
- `wisdom`: Wisdom score (1-30)
- `charisma`: Charisma score (1-30)

### Combat Stats
- `maxHitPoints`: Maximum hit points
- `currentHitPoints`: Current hit points
- `temporaryHitPoints`: Temporary hit points
- `armorClass`: Armor Class (AC)
- `initiative`: Initiative modifier
- `walkingSpeed`: Walking speed in feet
- `flyingSpeed`: Flying speed in feet (0 if none)
- `swimmingSpeed`: Swimming speed in feet (0 if none)
- `climbingSpeed`: Climbing speed in feet (0 if none)
- `hitDice`: Hit dice string (e.g., "3d10")
- `hitDiceUsed`: Number of hit dice used during short rest

### Proficiencies

**Saving Throws:**
```json
"proficientSavingThrows": ["STR", "DEX", "CON", "INT", "WIS", "CHA"]
```

**Skills:**
```json
"_proficientSkillsStrings": [
    "Acrobatics",
    "Animal Handling",
    "Arcana",
    "Athletics",
    "Deception",
    "History",
    "Insight",
    "Intimidation",
    "Investigation",
    "Medicine",
    "Nature",
    "Perception",
    "Performance",
    "Persuasion",
    "Religion",
    "Sleight of Hand",
    "Stealth",
    "Survival"
]
```

**Armor:**
```json
"proficientArmor": ["Light", "Medium", "Heavy", "Shields"]
```

**Weapons:**
```json
"proficientWeapons": ["Simple", "Martial"]
```

**Tools:**
```json
"proficientTools": ["Thieves' Tools", "Smith's Tools"]
```

**Languages:**
```json
"languages": ["Common", "Elvish", "Draconic"]
```

### Other
- `hasInspiration`: true/false
- `exhaustionLevel`: 0-6
- `conditions`: Array of condition strings (e.g., ["Poisoned", "Frightened"])
- `deathSaveSuccesses`: 0-3
- `deathSaveFailures`: 0-3

## Example: Level 1 Fighter

```json
{
    "characterName": "Thorin",
    "level": 1,
    "characterClass": "Fighter",
    "subclass": "",
    "race": "Dwarf",
    "subrace": "Mountain Dwarf",
    "background": "Soldier",
    "alignment": "Lawful Good",
    "strength": 16,
    "dexterity": 12,
    "constitution": 16,
    "intelligence": 10,
    "wisdom": 13,
    "charisma": 8,
    "maxHitPoints": 12,
    "currentHitPoints": 12,
    "armorClass": 18,
    "proficientSavingThrows": ["STR", "CON"],
    "_proficientSkillsStrings": ["Athletics", "Intimidation"],
    "proficientArmor": ["Light", "Medium", "Heavy", "Shields"],
    "proficientWeapons": ["Simple", "Martial"]
}
```

## Tips

1. **Start Simple**: Copy the example and modify values
2. **Use Templates**: Create templates for common classes/races
3. **Validate JSON**: Use a JSON validator (online tool) if you get errors
4. **File Location**: Must be in `StreamingAssets/Characters/` folder
5. **File Name**: Can be anything, but use `.json` extension

## Troubleshooting

### Character Not Loading?
- Check file path is correct
- Ensure file is in `StreamingAssets/Characters/`
- Validate JSON syntax (no trailing commas, proper quotes)
- Check Unity Console for errors

### Wrong Data Showing?
- Make sure JSON file is saved
- Restart Unity if needed
- Check that service initializer is pointing to correct file

### JSON Errors?
- Use a JSON validator
- Check for trailing commas
- Ensure all strings are in quotes
- Arrays use `[]`, objects use `{}`

