# Ruleset-Agnostic Architecture - Refactoring Guide

## Overview

This refactoring introduces a **ruleset-agnostic architecture** that allows the VTT to support multiple tabletop RPG rulesets (D&D 5e, Pathfinder, etc.) while adhering to SOLID principles and industry-standard design patterns.

## Architecture Principles

### SOLID Principles Applied

1. **Single Responsibility Principle (SRP)**
   - `IRulesetCalculator`: Only handles ruleset-specific calculations
   - `ICharacterDataAdapter`: Only handles data format conversion
   - `CharacterSheetUIMapper`: Only handles UI structure mapping
   - `CharacterSheetUIUpdater`: Only handles UI updates

2. **Open/Closed Principle (OCP)**
   - New rulesets can be added by implementing `IRulesetCalculator` and `ICharacterDataAdapter`
   - No modification to existing code required
   - Factory pattern allows registration of new rulesets

3. **Liskov Substitution Principle (LSP)**
   - Any `IRulesetCalculator` implementation can be used interchangeably
   - Any `ICharacterDataAdapter` implementation can be used interchangeably

4. **Interface Segregation Principle (ISP)**
   - `IRulesetCalculator` focuses only on calculation methods
   - `ICharacterDataAdapter` focuses only on data conversion methods
   - No client is forced to depend on methods it doesn't use

5. **Dependency Inversion Principle (DIP)**
   - High-level UI code depends on `IRulesetCalculator` abstraction, not concrete implementations
   - Factories provide dependency injection
   - Easy to test with mock implementations

## Design Patterns Used

### 1. Strategy Pattern
- **Purpose**: Encapsulate ruleset-specific calculation algorithms
- **Implementation**: `IRulesetCalculator` interface with concrete implementations
- **Benefit**: Easy to swap calculation logic without changing client code

### 2. Adapter Pattern
- **Purpose**: Convert ruleset-specific data formats to generic UI format
- **Implementation**: `ICharacterDataAdapter` interface
- **Benefit**: UI code doesn't need to know about ruleset-specific data structures

### 3. Factory Pattern
- **Purpose**: Create appropriate calculator/adapter instances
- **Implementation**: `RulesetCalculatorFactory` and `RulesetAdapterFactory`
- **Benefit**: Centralized creation logic, easy to extend

### 4. Mapper Pattern
- **Purpose**: Separate UI structure knowledge from update logic
- **Implementation**: `CharacterSheetUIMapper` static class
- **Benefit**: UI structure changes don't require changes to update logic

## Architecture Layers

```
┌─────────────────────────────────────────┐
│         UI Layer (Views)                │
│  CharacterSheetUIUpdater                │
│  CharacterSheetUIMapper                 │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│      Adapter Layer                       │
│  ICharacterDataAdapter                   │
│  DnD5eCharacterDataAdapter              │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│      Calculator Layer                    │
│  IRulesetCalculator                      │
│  DnD5eRulesetCalculator                  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│      Data Layer                          │
│  DnD5eCharacterData                      │
│  CharacterData (legacy)                  │
└──────────────────────────────────────────┘
```

## Key Components

### IRulesetCalculator
Interface for ruleset-specific calculations:
- Ability modifiers
- Proficiency bonuses
- Skill modifiers
- Saving throw modifiers
- Weapon attack/damage calculations
- Weapon properties lookup

### ICharacterDataAdapter
Interface for converting ruleset data to UI format:
- Adapts to generic `CharacterData`
- Extracts ability scores/modifiers
- Extracts skill modifiers
- Extracts weapon data

### Factories
- `RulesetCalculatorFactory`: Creates calculator instances
- `RulesetAdapterFactory`: Creates adapter instances
- Both support registration of new rulesets

### UI Components
- `CharacterSheetUIMapper`: Maps UI element names/structure
- `CharacterSheetUIUpdater`: Updates UI elements using calculator/adapter

## Adding a New Ruleset

### Step 1: Create Calculator
```csharp
public class Pathfinder2eRulesetCalculator : IRulesetCalculator
{
    public string RulesetId => "Pathfinder2e";
    
    // Implement all interface methods
    public int CalculateAbilityModifier(int abilityScore) { ... }
    // ... etc
}
```

### Step 2: Create Adapter
```csharp
public class Pathfinder2eCharacterDataAdapter : ICharacterDataAdapter
{
    public string RulesetId => "Pathfinder2e";
    
    // Implement all interface methods
    public CharacterData AdaptToCharacterData(object rulesetData) { ... }
    // ... etc
}
```

### Step 3: Register with Factories
```csharp
RulesetCalculatorFactory.RegisterCalculator(new Pathfinder2eRulesetCalculator());
RulesetAdapterFactory.RegisterAdapter(new Pathfinder2eCharacterDataAdapter());
```

### Step 4: Update UI Mapper (if needed)
If the UI structure is different, update `CharacterSheetUIMapper` to support the new structure.

## Testing Strategy

### Unit Tests
- Test each calculator independently
- Test each adapter independently
- Mock dependencies for isolated testing

### Integration Tests
- Test calculator + adapter together
- Test UI updater with different rulesets
- Test factory registration/retrieval

### Example Test Structure
```csharp
[Test]
public void DnD5eCalculator_CalculatesAbilityModifier_Correctly()
{
    var calculator = new DnD5eRulesetCalculator();
    int modifier = calculator.CalculateAbilityModifier(16);
    Assert.AreEqual(3, modifier);
}
```

## Migration Path

### Phase 1: Current State
- Keep existing `CharacterSheetUIUpdater` working
- Add new refactored version alongside

### Phase 2: Gradual Migration
- Update `InGameUIPresenter` to use refactored updater
- Test thoroughly
- Keep old code as fallback

### Phase 3: Cleanup
- Remove old `CharacterSheetUIUpdater` once migration is complete
- Remove legacy calculation methods

## Benefits

1. **Scalability**: Easy to add new rulesets
2. **Testability**: Each component can be tested independently
3. **Maintainability**: Clear separation of concerns
4. **Flexibility**: Can swap rulesets at runtime
5. **Extensibility**: Open for extension, closed for modification

## Future Enhancements

1. **Ruleset Detection**: Auto-detect ruleset from JSON metadata
2. **Plugin System**: Load rulesets from external assemblies
3. **Ruleset Validation**: Validate character data against ruleset rules
4. **Ruleset-Specific UI**: Different UI layouts per ruleset
5. **Ruleset Conversion**: Convert characters between rulesets

