# Architecture Review & Cleanup Plan

## Current State Analysis

### ✅ Active Components (Keep)

1. **Core Services**
   - `IPlayerDataService` - Interface for data access
   - `JsonPlayerDataService` - JSON-based implementation (actively used)
   - `LocalPlayerDataService` - ScriptableObject-based (fallback)
   - `PlayerDataServiceLocator` - Service locator pattern
   - `PlayerDataServiceInitializer` - Scene initialization

2. **Data Models**
   - `DnD5eCharacterData` - Comprehensive 5e data model (actively used)
   - `CharacterData` - Legacy model (still used for UI compatibility)
   - `DnD5eSkill` - Enum for type-safe skills

3. **Ruleset System (New Architecture)**
   - `IRulesetCalculator` - Calculation interface
   - `DnD5eRulesetCalculator` - 5e implementation
   - `ICharacterDataAdapter` - Data conversion interface
   - `DnD5eCharacterDataAdapter` - 5e adapter
   - `RulesetCalculatorFactory` - Calculator factory
   - `RulesetAdapterFactory` - Adapter factory

4. **UI Components**
   - `CharacterSheetUIUpdater` - **Currently active** (used in InGameUIPresenter)
   - `CharacterSheetUIUpdaterRefactored` - **New version** (ready but not integrated)
   - `CharacterSheetUIMapper` - UI structure mapping

5. **Utilities**
   - `PlayerDataJsonLoader` - JSON file I/O
   - `PlayerDataFilePaths` - Path management
   - `DnD5eWeaponCalculator` - Weapon calculations (used by new system)

### ⚠️ Potentially Unused Components

1. **DnD5eCombatCalculator**
   - **Status**: Defined but not actively used
   - **Methods**: `CalculateMaxHP()`, `CalculateBaseAC()`, `CalculateInitiative()`, `CalculateHitDice()`
   - **Usage**: Only referenced in documentation (`DND5E_CALCULATIONS.md`)
   - **Decision**: **KEEP** - Useful for future features (HP calculation, AC calculation)

2. **WeaponData.GetWeaponData(CharacterData)**
   - **Status**: Legacy overload
   - **Usage**: Still used in `CharacterSheetUIUpdater` (old version)
   - **Decision**: **KEEP** - Needed for backward compatibility until migration

3. **CharacterSheetUIUpdater (Old Version)**
   - **Status**: Currently active in `InGameUIPresenter`
   - **Usage**: Line 402, 408 - `UpdateCharacterSheetUI()` calls it
   - **Decision**: **MIGRATE** - Should switch to refactored version

### 🔄 Duplicate/Redundant Code

1. **Weapon Calculation Logic**
   - `WeaponData.GetWeaponData()` has two implementations:
     - Legacy: Hardcoded switch statement (Longsword, Shortbow only)
     - New: Uses `DnD5eWeaponCalculator.CalculateWeaponData()`
   - **Issue**: Legacy version is limited and doesn't check proficiency properly
   - **Action**: Migrate to refactored updater which uses proper calculator

2. **UI Update Logic**
   - `CharacterSheetUIUpdater` - Old, hardcoded
   - `CharacterSheetUIUpdaterRefactored` - New, ruleset-agnostic
   - **Action**: Complete migration to refactored version

## Cleanup Recommendations

### Phase 1: Migration (High Priority)

1. **Migrate InGameUIPresenter to Refactored Updater**
   - Replace `CharacterSheetUIUpdater` with `CharacterSheetUIUpdaterRefactored`
   - Test thoroughly
   - Remove old `CharacterSheetUIUpdater` once verified

2. **Remove Legacy WeaponData Method**
   - After migration, remove `WeaponData.GetWeaponData(CharacterData)` overload
   - Keep only `WeaponData.GetWeaponData(string, DnD5eCharacterData)`

### Phase 2: Consolidation (Medium Priority)

1. **Integrate DnD5eCombatCalculator into Ruleset System**
   - Move `CalculateMaxHP()`, `CalculateBaseAC()` to `IRulesetCalculator`
   - Update `DnD5eRulesetCalculator` to implement these
   - Remove standalone `DnD5eCombatCalculator` class

2. **Simplify WeaponData Class**
   - After migration, `WeaponData` becomes a pure data class
   - Remove static `GetWeaponData()` methods
   - Use `IRulesetCalculator` directly for weapon calculations

### Phase 3: Documentation Cleanup (Low Priority)

1. **Update Documentation**
   - Mark old architecture docs as deprecated
   - Update guides to reference new architecture
   - Consolidate overlapping documentation

## Architectural Improvements

### 1. Remove Direct Dependencies on Legacy CharacterData

**Current Issue**: UI still depends on `CharacterData` (legacy model)

**Solution**: 
- Complete migration to ruleset-agnostic system
- UI should only know about `CharacterData` through adapters
- Eventually, UI could work directly with ruleset data through adapters

### 2. Consolidate Calculation Logic

**Current Issue**: Calculations scattered across multiple classes:
- `DnD5eWeaponCalculator` - Weapon calculations
- `DnD5eCombatCalculator` - Combat calculations
- `DnD5eCharacterData` - Some calculations in properties

**Solution**:
- All calculations should go through `IRulesetCalculator`
- `DnD5eRulesetCalculator` should delegate to existing calculators
- Eventually, consolidate all into ruleset calculator

### 3. Improve Testability

**Current State**: Some static methods, hard to mock

**Improvements**:
- ✅ Ruleset system uses interfaces (testable)
- ⚠️ Some legacy code still uses static methods
- **Action**: Continue migration to interface-based design

### 4. Reduce Code Duplication

**Issues Found**:
- Skill mapping duplicated in `CharacterSheetUIUpdater` and `CharacterSheetUIMapper`
- Weapon name mapping hardcoded in multiple places

**Solution**:
- ✅ `CharacterSheetUIMapper` centralizes UI mappings
- Use mapper consistently across all UI code

## File Organization

### Current Structure
```
PlayerData/
├── Core Services/
├── Data Models/
├── Calculators/ (DnD5eWeaponCalculator, DnD5eCombatCalculator)
├── Rulesets/ (New architecture)
└── Documentation/
```

### Recommended Structure
```
PlayerData/
├── Core Services/
├── Data Models/
├── Rulesets/
│   ├── Interfaces/
│   ├── DnD5e/
│   │   ├── Calculator/
│   │   ├── Adapter/
│   │   └── Data/
│   └── Factories/
└── Documentation/
```

**Benefit**: Clearer organization, easier to add new rulesets

## Migration Checklist

### Step 1: Update InGameUIPresenter
- [ ] Replace `CharacterSheetUIUpdater` with `CharacterSheetUIUpdaterRefactored`
- [ ] Update method calls
- [ ] Test UI updates correctly

### Step 2: Remove Old Updater
- [ ] Verify refactored version works
- [ ] Delete `CharacterSheetUIUpdater.cs`
- [ ] Update any remaining references

### Step 3: Clean Up WeaponData
- [ ] Remove legacy `GetWeaponData(CharacterData)` overload
- [ ] Update any remaining callers
- [ ] Consider making `WeaponData` a pure data class

### Step 4: Consolidate Calculators
- [ ] Move combat calculations to `IRulesetCalculator`
- [ ] Update `DnD5eRulesetCalculator`
- [ ] Remove `DnD5eCombatCalculator` (or keep as helper if needed)

## Risk Assessment

### Low Risk
- Removing `DnD5eCombatCalculator` (not actively used)
- Removing old `CharacterSheetUIUpdater` (after migration)

### Medium Risk
- Removing legacy `WeaponData.GetWeaponData()` (ensure all callers updated)
- Migrating `InGameUIPresenter` (needs thorough testing)

### High Risk
- None identified

## Benefits of Cleanup

1. **Reduced Complexity**: Less duplicate code, clearer architecture
2. **Better Maintainability**: Single source of truth for calculations
3. **Easier Testing**: Interface-based design is more testable
4. **Future-Proof**: Ruleset system ready for expansion
5. **Code Quality**: Follows SOLID principles consistently

## Next Steps

1. **Immediate**: Create migration plan for `InGameUIPresenter`
2. **Short-term**: Execute migration and test
3. **Medium-term**: Clean up legacy code
4. **Long-term**: Reorganize file structure

