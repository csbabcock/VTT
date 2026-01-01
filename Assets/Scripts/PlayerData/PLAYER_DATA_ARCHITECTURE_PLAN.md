# Player Data Architecture Plan

## Overview
This document outlines the architecture for a multiplayer-ready player data system that adheres to SOLID principles and integrates seamlessly with the existing codebase.

## Goals
1. **Multiplayer Support**: Design for network synchronization from the start
2. **SOLID Principles**: Follow Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion
3. **Backward Compatibility**: Don't break existing functionality
4. **Extensibility**: Easy to add new player data fields and features
5. **Industry Standards**: Use proven design patterns (Repository, Observer, Command, etc.)

## Current State Analysis

### Existing Architecture
- **UI Pattern**: MVP (Model-View-Presenter)
- **CharacterData**: Simple POCO class with hardcoded values
- **Services**: Direct instantiation (no DI framework yet)
- **Interfaces**: Used in PlayerController components (good pattern to follow)
- **Singleton**: UIInteractionService uses singleton pattern

### Current Limitations
1. `CharacterData` is a simple class with no network support
2. Data is hardcoded, not loaded from a source
3. No player identification system
4. No data persistence
5. No event system for data changes
6. Direct instantiation in Presenter (tight coupling)

## Proposed Architecture

### 1. Core Data Layer

#### 1.1 Player Data Model (Immutable Snapshot)
```csharp
// Immutable data structure for network synchronization
public struct PlayerDataSnapshot
{
    public ulong PlayerId { get; }
    public string CharacterName { get; }
    public AbilityScores AbilityScores { get; }
    public int Level { get; }
    public int ProficiencyBonus { get; }
    public HashSet<string> ProficientSkills { get; }
    // ... other fields
}
```

**Why Immutable?**
- Thread-safe for network synchronization
- Prevents accidental mutations
- Clear ownership of data

#### 1.2 Mutable Player Data (Source of Truth)
```csharp
// Mutable class for local modifications
public class PlayerData
{
    public ulong PlayerId { get; set; }
    public string CharacterName { get; set; }
    public AbilityScores AbilityScores { get; set; }
    // ... other fields
    
    public PlayerDataSnapshot CreateSnapshot() { ... }
    public void ApplySnapshot(PlayerDataSnapshot snapshot) { ... }
}
```

**Separation of Concerns:**
- `PlayerData`: Mutable source of truth (local)
- `PlayerDataSnapshot`: Immutable network representation

### 2. Repository Pattern (Data Access)

#### 2.1 Interface
```csharp
public interface IPlayerDataRepository
{
    PlayerData GetPlayerData(ulong playerId);
    PlayerData GetLocalPlayerData();
    void SavePlayerData(PlayerData data);
    bool HasPlayerData(ulong playerId);
    IEnumerable<PlayerData> GetAllPlayerData();
}
```

#### 2.2 Implementations
- **LocalPlayerDataRepository**: For single-player or local player
- **NetworkPlayerDataRepository**: For multiplayer (wraps network layer)
- **MockPlayerDataRepository**: For testing

**Benefits:**
- **Dependency Inversion**: Code depends on interface, not implementation
- **Testability**: Easy to mock for unit tests
- **Flexibility**: Can swap implementations without changing consumers

### 3. Service Layer (Business Logic)

#### 3.1 Player Data Service
```csharp
public interface IPlayerDataService
{
    event Action<ulong, PlayerDataSnapshot> PlayerDataChanged;
    
    PlayerData GetPlayerData(ulong playerId);
    PlayerData GetLocalPlayerData();
    void UpdatePlayerData(ulong playerId, Action<PlayerData> updateAction);
    void SubscribeToPlayerData(ulong playerId, Action<PlayerDataSnapshot> callback);
    void UnsubscribeFromPlayerData(ulong playerId, Action<PlayerDataSnapshot> callback);
}
```

**Responsibilities:**
- Manages player data lifecycle
- Provides events for data changes (Observer pattern)
- Coordinates between repository and network layer
- Validates data changes

**Why Service Layer?**
- **Single Responsibility**: Handles business logic only
- **Open/Closed**: Can extend with new features without modifying core
- **Separation**: Business logic separate from data access

### 4. Network Layer (Multiplayer Support)

#### 4.1 Network Interface (Abstraction)
```csharp
public interface INetworkPlayerDataSync
{
    bool IsNetworked { get; }
    void SendPlayerDataUpdate(ulong playerId, PlayerDataSnapshot snapshot);
    void RequestPlayerData(ulong playerId);
    event Action<ulong, PlayerDataSnapshot> PlayerDataReceived;
}
```

#### 4.2 Implementations
- **NullNetworkSync**: No-op implementation for single-player
- **UnityNetcodeSync**: Implementation using Unity Netcode (when added)
- **MirrorSync**: Implementation using Mirror (if using Mirror)
- **CustomSync**: Custom networking solution

**Benefits:**
- **Network Agnostic**: Core code doesn't depend on specific networking library
- **Easy Testing**: Can test without network
- **Flexible**: Can swap networking solutions

### 5. Observer Pattern (Event System)

#### 5.1 Data Change Events
```csharp
public class PlayerDataChangedEventArgs : EventArgs
{
    public ulong PlayerId { get; }
    public PlayerDataSnapshot PreviousSnapshot { get; }
    public PlayerDataSnapshot NewSnapshot { get; }
    public string ChangedField { get; }
}
```

**Why Events?**
- **Loose Coupling**: UI doesn't need direct reference to data
- **Reactive**: UI updates automatically when data changes
- **Multiple Listeners**: Multiple systems can react to changes

### 6. Command Pattern (Undo/Redo Support)

#### 6.1 Player Data Commands
```csharp
public interface IPlayerDataCommand
{
    void Execute();
    void Undo();
    bool CanExecute();
}
```

**Example Commands:**
- `UpdateAbilityScoreCommand`
- `AddSkillProficiencyCommand`
- `LevelUpCommand`

**Benefits:**
- **Undo/Redo**: Can implement history
- **Validation**: Commands can validate before execution
- **Network Sync**: Commands can be serialized and sent over network

### 7. Integration with Existing Code

#### 7.1 Backward Compatibility Strategy

**Phase 1: Add New System Alongside Old**
- Keep `CharacterData` class working
- Create adapter: `CharacterDataAdapter : IPlayerDataService`
- Gradually migrate UI to use new system

**Phase 2: Migration**
- Update `InGameUIPresenter` to use `IPlayerDataService`
- Update UI to subscribe to data change events
- Remove direct `CharacterData` instantiation

**Phase 3: Cleanup**
- Remove old `CharacterData` class (or keep as legacy support)

#### 7.2 Adapter Pattern
```csharp
public class CharacterDataAdapter
{
    private readonly IPlayerDataService _playerDataService;
    
    public CharacterData GetCharacterData()
    {
        var playerData = _playerDataService.GetLocalPlayerData();
        return ConvertToCharacterData(playerData);
    }
}
```

**Why Adapter?**
- **Backward Compatibility**: Existing code continues to work
- **Gradual Migration**: Can migrate piece by piece
- **No Breaking Changes**: Old code doesn't need immediate updates

## SOLID Principles Adherence

### Single Responsibility Principle (SRP) ✅
- **PlayerData**: Only holds data
- **PlayerDataService**: Only manages data lifecycle
- **PlayerDataRepository**: Only handles data persistence
- **NetworkSync**: Only handles network communication
- **Commands**: Only handle specific data modifications

### Open/Closed Principle (OCP) ✅
- **Interfaces**: Can extend with new implementations without modifying existing code
- **Commands**: Can add new command types without changing command processor
- **Repository**: Can add new repository types (e.g., DatabaseRepository) without changing service

### Liskov Substitution Principle (LSP) ✅
- **Repository Implementations**: Any IPlayerDataRepository can replace another
- **Network Sync Implementations**: Any INetworkPlayerDataSync can replace another
- **Service Implementations**: Any IPlayerDataService can replace another

### Interface Segregation Principle (ISP) ✅
- **Focused Interfaces**: Each interface has a single, focused purpose
- **No Fat Interfaces**: Interfaces don't force implementers to provide unused methods
- **Client-Specific**: Interfaces tailored to what clients need

### Dependency Inversion Principle (DIP) ✅
- **High-Level Depends on Abstractions**: Services depend on interfaces, not concrete classes
- **Low-Level Implements Abstractions**: Concrete classes implement interfaces
- **Dependency Injection**: Dependencies injected, not created directly

## Design Patterns Used

### 1. Repository Pattern
- **Purpose**: Abstract data access
- **Benefit**: Easy to swap data sources (local, network, database)

### 2. Service Pattern
- **Purpose**: Encapsulate business logic
- **Benefit**: Single point of access, easy to test

### 3. Observer Pattern
- **Purpose**: Notify listeners of data changes
- **Benefit**: Loose coupling, reactive updates

### 4. Command Pattern
- **Purpose**: Encapsulate data modifications
- **Benefit**: Undo/redo, network serialization, validation

### 5. Adapter Pattern
- **Purpose**: Bridge old and new systems
- **Benefit**: Backward compatibility, gradual migration

### 6. Strategy Pattern
- **Purpose**: Interchangeable algorithms (network sync strategies)
- **Benefit**: Can swap networking solutions

### 7. Factory Pattern
- **Purpose**: Create appropriate repository/service based on context
- **Benefit**: Single-player vs multiplayer handled transparently

## Network Synchronization Strategy

### Approach: State Synchronization
1. **Snapshot-Based**: Send complete state snapshots (simpler, more reliable)
2. **Delta Updates**: Send only changed fields (more efficient, more complex)
3. **Hybrid**: Use snapshots for initial sync, deltas for updates

### Recommended: Hybrid Approach
- **Initial Sync**: Full snapshot when player joins
- **Updates**: Delta updates for changed fields
- **Periodic Sync**: Full snapshot every N seconds (safety net)

### Network Events
```csharp
// Client -> Server
- RequestPlayerData(playerId)
- UpdatePlayerData(playerId, snapshot)

// Server -> Client
- PlayerDataReceived(playerId, snapshot)
- PlayerJoined(playerId)
- PlayerLeft(playerId)
```

## File Structure

```
Assets/Scripts/PlayerData/
├── Core/
│   ├── PlayerData.cs                    # Mutable player data
│   ├── PlayerDataSnapshot.cs            # Immutable snapshot
│   ├── AbilityScores.cs                 # Value object
│   └── PlayerDataConstants.cs           # Constants
├── Repository/
│   ├── IPlayerDataRepository.cs         # Interface
│   ├── LocalPlayerDataRepository.cs     # Single-player implementation
│   ├── NetworkPlayerDataRepository.cs   # Multiplayer implementation
│   └── MockPlayerDataRepository.cs       # Testing implementation
├── Services/
│   ├── IPlayerDataService.cs             # Service interface
│   ├── PlayerDataService.cs              # Service implementation
│   └── PlayerDataValidator.cs            # Validation logic
├── Network/
│   ├── INetworkPlayerDataSync.cs         # Network interface
│   ├── NullNetworkSync.cs                # No-op for single-player
│   └── UnityNetcodeSync.cs               # Unity Netcode implementation (future)
├── Commands/
│   ├── IPlayerDataCommand.cs             # Command interface
│   ├── UpdateAbilityScoreCommand.cs      # Example command
│   └── CommandProcessor.cs               # Executes commands
├── Events/
│   ├── PlayerDataChangedEventArgs.cs      # Event args
│   └── PlayerDataEvents.cs               # Event definitions
├── Adapters/
│   └── CharacterDataAdapter.cs           # Bridges old and new systems
└── Factories/
    └── PlayerDataServiceFactory.cs        # Creates appropriate service
```

## Implementation Phases

### Phase 1: Core Data Structures (Week 1)
- [ ] Create `PlayerData` and `PlayerDataSnapshot`
- [ ] Create `AbilityScores` value object
- [ ] Create `IPlayerDataRepository` interface
- [ ] Implement `LocalPlayerDataRepository`
- [ ] Unit tests for data structures

### Phase 2: Service Layer (Week 1-2)
- [ ] Create `IPlayerDataService` interface
- [ ] Implement `PlayerDataService`
- [ ] Add event system for data changes
- [ ] Unit tests for service

### Phase 3: Integration (Week 2)
- [ ] Create `CharacterDataAdapter`
- [ ] Update `InGameUIPresenter` to use service (optional, can use adapter)
- [ ] Ensure backward compatibility
- [ ] Integration tests

### Phase 4: Network Abstraction (Week 2-3)
- [ ] Create `INetworkPlayerDataSync` interface
- [ ] Implement `NullNetworkSync` (for single-player)
- [ ] Create network event system
- [ ] Unit tests for network layer

### Phase 5: Command Pattern (Week 3, Optional)
- [ ] Create command interfaces
- [ ] Implement example commands
- [ ] Add command processor
- [ ] Unit tests

### Phase 6: Network Implementation (Future)
- [ ] Implement `UnityNetcodeSync` when networking library is chosen
- [ ] Add network serialization
- [ ] Add network validation
- [ ] Integration tests

## Testing Strategy

### Unit Tests
- **Data Structures**: Test immutability, equality, serialization
- **Repository**: Test CRUD operations, edge cases
- **Service**: Test business logic, event firing
- **Commands**: Test execution, undo, validation

### Integration Tests
- **Service + Repository**: Test full data flow
- **Service + Network**: Test network synchronization
- **UI Integration**: Test UI updates on data changes

### Mock Strategy
- Use interfaces for all dependencies
- Create mock implementations for testing
- Test in isolation without Unity (pure C#)

## Migration Path

### Step 1: Add New System (No Breaking Changes)
1. Create all new files
2. Implement `LocalPlayerDataRepository` with default data
3. Create `CharacterDataAdapter` that wraps new system
4. Test that adapter produces same results as old `CharacterData`

### Step 2: Optional Integration
1. Update `InGameUIPresenter` to optionally use new service
2. Keep old `CharacterData` instantiation as fallback
3. Test both paths work

### Step 3: Full Migration (When Ready)
1. Update all UI code to use new service
2. Remove old `CharacterData` instantiation
3. Remove adapter (or keep for legacy support)

## Performance Considerations

### Optimization Strategies
1. **Caching**: Cache player data snapshots
2. **Lazy Loading**: Load player data on demand
3. **Delta Updates**: Only send changed fields over network
4. **Batching**: Batch multiple updates together
5. **Compression**: Compress network messages (if needed)

### Memory Management
- Use structs for immutable snapshots (value types)
- Use object pooling for frequently created objects
- Dispose event subscriptions properly

## Security Considerations

### Data Validation
- Validate all data changes on server
- Sanitize input from clients
- Check permissions before allowing modifications

### Network Security
- Encrypt sensitive data in transit
- Authenticate players
- Rate limit updates to prevent abuse

## Future Enhancements

### Potential Additions
1. **Persistence**: Save/load from file or database
2. **Versioning**: Handle data schema changes
3. **Conflict Resolution**: Handle concurrent modifications
4. **Caching**: Client-side caching with invalidation
5. **Compression**: Compress network messages
6. **Encryption**: Encrypt sensitive player data

## Conclusion

This architecture provides:
- ✅ **SOLID Principles**: All principles adhered to
- ✅ **Multiplayer Ready**: Network abstraction from the start
- ✅ **Backward Compatible**: Can migrate gradually
- ✅ **Testable**: All components can be unit tested
- ✅ **Extensible**: Easy to add new features
- ✅ **Industry Standards**: Uses proven design patterns

The system is designed to grow with the project, starting simple and adding complexity only when needed.

