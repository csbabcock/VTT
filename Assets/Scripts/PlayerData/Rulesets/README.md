# Ruleset System - Overview

This directory contains the ruleset-agnostic architecture for supporting multiple tabletop RPG systems.

## Quick Start

### Using the Refactored System

The refactored system is available in `CharacterSheetUIUpdaterRefactored`. To use it:

```csharp
// In InGameUIPresenter
CharacterSheetUIUpdaterRefactored.UpdateCharacterSheet(
    _view.Root, 
    characterData, 
    "DnD5e" // ruleset ID
);
```

### Current Status

- ✅ **D&D 5e Support**: Fully implemented
- ⏳ **Pathfinder Support**: Ready to implement (architecture supports it)
- ⏳ **Other Rulesets**: Architecture ready for extension

## Architecture

See [REFACTORING_ARCHITECTURE.md](./REFACTORING_ARCHITECTURE.md) for detailed architecture documentation.

## Files

- `IRulesetCalculator.cs` - Interface for ruleset calculations
- `DnD5eRulesetCalculator.cs` - D&D 5e implementation
- `ICharacterDataAdapter.cs` - Interface for data conversion
- `DnD5eCharacterDataAdapter.cs` - D&D 5e adapter
- `RulesetCalculatorFactory.cs` - Factory for calculators
- `RulesetAdapterFactory.cs` - Factory for adapters

## Migration

The old `CharacterSheetUIUpdater` still works. The refactored version (`CharacterSheetUIUpdaterRefactored`) is ready to use but not yet integrated into the presenter. This allows for gradual migration and testing.

## Testing

Each component can be unit tested independently:

```csharp
// Test calculator
var calculator = new DnD5eRulesetCalculator();
Assert.AreEqual(3, calculator.CalculateAbilityModifier(16));

// Test adapter
var adapter = new DnD5eCharacterDataAdapter();
var characterData = adapter.AdaptToCharacterData(dnD5eData);
```

## Adding a New Ruleset

1. Implement `IRulesetCalculator`
2. Implement `ICharacterDataAdapter`
3. Register with factories
4. Update UI mapper if needed

See [REFACTORING_ARCHITECTURE.md](./REFACTORING_ARCHITECTURE.md) for detailed steps.

