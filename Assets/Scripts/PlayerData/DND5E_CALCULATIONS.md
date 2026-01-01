# D&D 5e Calculation Rules - Implementation Guide

## Overview
This document explains how all D&D 5e calculations are implemented and verified against the JSON character data.

## ✅ Implemented Calculations

### 1. Ability Modifiers
**Formula**: `(Ability Score - 10) / 2` (rounded down)

**Example**: 
- STR 16 → (16 - 10) / 2 = +3
- DEX 14 → (14 - 10) / 2 = +2
- INT 10 → (10 - 10) / 2 = +0

**Location**: `DnD5eCharacterData.CalculateModifier()`

### 2. Proficiency Bonus
**Formula**: `(Level - 1) / 4 + 2` (rounded down)

**Levels**:
- 1-4: +2
- 5-8: +3
- 9-12: +4
- 13-16: +5
- 17-20: +6

**Location**: `DnD5eCharacterData.CalculateProficiencyBonus()`

### 3. Skill Modifiers
**Formula**: `Ability Modifier + (Proficiency Bonus if proficient)`

**Example**:
- Athletics (STR) with proficiency: +3 (STR) + 2 (proficiency) = +5
- Athletics (STR) without proficiency: +3 (STR) = +3

**Location**: `DnD5eCharacterData.GetSkillModifier()`

### 4. Saving Throw Modifiers
**Formula**: `Ability Modifier + (Proficiency Bonus if proficient)`

**Example**:
- STR save with proficiency: +3 (STR) + 2 (proficiency) = +5
- STR save without proficiency: +3 (STR) = +3

**Location**: `DnD5eCharacterData.GetSavingThrowModifier()`

### 5. Attack Rolls
**Formula**: `d20 + Ability Modifier + (Proficiency Bonus if proficient with weapon)`

**Rules**:
- Melee weapons use STR modifier (unless finesse)
- Ranged weapons use DEX modifier
- Finesse weapons can use STR or DEX (whichever is higher)
- Must be proficient with weapon to add proficiency bonus

**Example**:
- Longsword (STR 16, proficient): d20 + 3 (STR) + 2 (proficiency) = d20 + 5
- Shortbow (DEX 14, proficient): d20 + 2 (DEX) + 2 (proficiency) = d20 + 4
- Longsword (STR 16, NOT proficient): d20 + 3 (STR) = d20 + 3

**Location**: `DnD5eWeaponCalculator.CalculateWeaponData()`

### 6. Damage Rolls
**Formula**: `Weapon Damage Dice + Ability Modifier`

**Rules**:
- Melee weapons add STR modifier (unless finesse, then STR or DEX)
- Ranged weapons add DEX modifier (unless thrown, then STR)
- Finesse weapons can use STR or DEX (whichever is higher)
- **Proficiency does NOT affect damage** (only attack rolls)

**Example**:
- Longsword (STR 16): 1d8 + 3 (STR)
- Shortbow (DEX 14): 1d6 + 2 (DEX)
- Rapier (finesse, STR 16, DEX 14): 1d8 + 3 (uses STR, higher)

**Location**: `DnD5eWeaponCalculator.CalculateWeaponData()`

### 7. Initiative
**Formula**: `DEX Modifier`

**Example**:
- DEX 14 → Initiative = +2

**Location**: `DnD5eCharacterData.initiativeModifier` (calculated property)

### 8. Hit Points (Optional Calculation)
**Formula**: `(Hit Die + CON modifier) + (Average Hit Die + CON modifier) × (Level - 1)`

**Hit Die by Class**:
- Barbarian: d12
- Fighter, Paladin, Ranger: d10
- Artificer, Bard, Cleric, Druid, Monk, Rogue, Warlock: d8
- Sorcerer, Wizard: d6

**Example** (Fighter 3, CON 14):
- Level 1: 10 (d10) + 2 (CON) = 12
- Levels 2-3: 2 × (6 average + 2 CON) = 16
- Total: 12 + 16 = 28 HP

**Note**: HP is usually manually set, but can be calculated.

**Location**: Can be added to `IRulesetCalculator` interface if needed in future.

### 9. Armor Class (Simplified)
**Formula**: Varies by armor type:
- Unarmored: 10 + DEX modifier
- Light Armor: 11 + DEX modifier
- Medium Armor: 13 + DEX modifier (max +2)
- Heavy Armor: Fixed (varies by armor)

**Note**: AC is usually manually set in character data.

**Location**: Can be added to `IRulesetCalculator` interface if needed in future.

## Weapon Proficiency Checking

The system checks proficiency in this order:
1. Exact weapon name in `proficientWeapons` list
2. Weapon category ("Simple" or "Martial") in `proficientWeapons` list
3. "Simple" or "Martial" string in `proficientWeapons` list

**Example JSON**:
```json
"proficientWeapons": ["Simple", "Martial", "Longsword"]
```
This character is proficient with:
- All simple weapons
- All martial weapons
- Longsword specifically (redundant but allowed)

## Finesse Weapons

Finesse weapons can use STR or DEX modifier (whichever is higher):
- Dagger
- Dart
- Rapier
- Scimitar
- Shortsword
- Whip

**Calculation**: System automatically uses the higher modifier.

## Ranged Weapons

Ranged weapons always use DEX modifier for attack and damage:
- Shortbow, Longbow
- Crossbows
- Thrown weapons (can use STR if melee weapon)

## Data Flow

1. **JSON File** → Loaded by `JsonPlayerDataService`
2. **DnD5eCharacterData** → Contains all character stats
3. **Calculations** → Use `DnD5eCharacterData` methods
4. **Weapon Data** → `DnD5eWeaponCalculator` calculates attack/damage
5. **UI Updates** → `CharacterSheetUIUpdater` displays calculated values

## Verification Checklist

- [x] Ability modifiers calculated correctly
- [x] Proficiency bonus based on level
- [x] Skills include proficiency if proficient
- [x] Saving throws include proficiency if proficient
- [x] Attack rolls check weapon proficiency
- [x] Damage rolls use correct ability modifier
- [x] Finesse weapons use higher of STR/DEX
- [x] Ranged weapons use DEX
- [x] Initiative uses DEX modifier
- [ ] HP calculation (optional, usually manual)
- [ ] AC calculation (optional, usually manual)

## Testing

To verify calculations are correct:

1. **Create test character** with known stats
2. **Check ability modifiers** match expected values
3. **Check skill modifiers** include proficiency correctly
4. **Check attack rolls** use correct modifiers and proficiency
5. **Check damage rolls** use correct modifiers (no proficiency)
6. **Verify weapon proficiency** affects attack bonus

## Example Test Case

**Character**: Level 3 Fighter, STR 16, DEX 14, CON 14
- Proficiency Bonus: +2 (levels 1-4)
- STR Modifier: +3
- DEX Modifier: +2
- CON Modifier: +2

**Longsword Attack** (proficient):
- Attack: d20 + 3 (STR) + 2 (proficiency) = d20 + 5 ✅
- Damage: 1d8 + 3 (STR) = 1d8 + 3 ✅

**Shortbow Attack** (proficient):
- Attack: d20 + 2 (DEX) + 2 (proficiency) = d20 + 4 ✅
- Damage: 1d6 + 2 (DEX) = 1d6 + 2 ✅

**Athletics Skill** (proficient):
- Modifier: +3 (STR) + 2 (proficiency) = +5 ✅

**STR Saving Throw** (proficient):
- Modifier: +3 (STR) + 2 (proficiency) = +5 ✅

