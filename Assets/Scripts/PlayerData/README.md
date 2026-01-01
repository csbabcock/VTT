# Player Data System - Vertical Slice

## Overview
A simple, working system for the UI to load and display player data. Designed for single-player/local use now, but structured so networking can be added later without breaking changes.

## What Was Created

### Core Components

1. **`IPlayerDataService`** - Interface for accessing player data
   - `GetPlayerData()` - Gets current player's character data
   - `PlayerDataChanged` event - Fired when data changes
   - `UpdatePlayerData()` - Updates data and triggers event

2. **`LocalPlayerDataService`** - Local implementation
   - Loads data from ScriptableObject (if provided) or uses defaults
   - Fires events when data changes
   - Ready to be swapped with network implementation later

3. **`PlayerDataAsset`** - ScriptableObject for character data
   - Designer-friendly: Create character presets in Unity
   - No code changes needed to test different characters
   - Can be assigned to service or loaded dynamically

4. **`PlayerDataServiceLocator`** - Simple service access
   - `PlayerDataServiceLocator.Service` - Get the service
   - Can inject custom implementations for testing
   - No dependency injection framework needed

### Integration

- **`InGameUIPresenter`** now uses the service instead of directly creating `CharacterData`
- All references updated to get data from service
- Subscribes to data change events (ready for reactive UI updates)

## How to Use

### Setup in Scene (Recommended)

1. **Create a Character Preset:**
   - In Unity, right-click in Project window
   - Select `Create > Game > Player Data`
   - Configure the character stats (name, ability scores, etc.)
   - Name it something like "Arlen_CharacterData"

2. **Add to Scene:**
   - Create an empty GameObject in your scene (or use an existing one like a GameManager)
   - Add the `PlayerDataServiceInitializer` component
   - Drag your PlayerData ScriptableObject into the "Player Data Asset" field
   - The service will automatically initialize when the scene starts

3. **That's it!** The UI will now use this character data.

### Basic Usage (Code)

```csharp
// Get player data
var service = PlayerDataServiceLocator.Service;
var playerData = service.GetPlayerData();

// Use the data
string name = playerData.CharacterName;
int strength = playerData.Strength;
```

### Creating Character Presets

1. In Unity, right-click in Project window
2. Select `Create > Game > Player Data`
3. Configure the character stats
4. Assign to `PlayerDataServiceInitializer` in your scene

### Updating Data

```csharp
var service = PlayerDataServiceLocator.Service;

// Update data (triggers PlayerDataChanged event)
service.UpdatePlayerData(data => 
{
    data.Strength = 18;
    data.CharacterName = "New Name";
});
```

### Subscribing to Changes

```csharp
var service = PlayerDataServiceLocator.Service;

// Subscribe to data changes
service.PlayerDataChanged += OnDataChanged;

void OnDataChanged(CharacterData data)
{
    // Update UI when data changes
    UpdateUI(data);
}
```

## Architecture Benefits

### SOLID Principles ✅
- **Single Responsibility**: Each class has one clear purpose
- **Open/Closed**: Can extend with new implementations without modifying existing code
- **Liskov Substitution**: Any `IPlayerDataService` implementation can replace another
- **Interface Segregation**: Focused, client-specific interface
- **Dependency Inversion**: UI depends on interface, not concrete class

### Design Patterns Used
- **Service Pattern**: Encapsulates data access logic
- **Service Locator**: Simple access point (can upgrade to DI later)
- **Observer Pattern**: Event-driven updates
- **Strategy Pattern**: Can swap implementations (local → network)

## Future Extensions

### When Adding Networking:
1. Create `NetworkPlayerDataService : IPlayerDataService`
2. Swap in: `PlayerDataServiceLocator.Service = new NetworkPlayerDataService()`
3. UI code doesn't need to change!

### When Adding Persistence:
1. Add `SavePlayerData()` to interface
2. Implement in service
3. Save to file/cloud when needed

### When Adding Multiple Players:
1. Change interface to `GetPlayerData(ulong playerId)`
2. Service manages dictionary of players
3. UI requests specific player

## File Structure

```
Assets/Scripts/PlayerData/
├── IPlayerDataService.cs              # Interface
├── LocalPlayerDataService.cs          # Local implementation
├── PlayerDataServiceLocator.cs        # Service access
├── PlayerDataAsset.cs                  # ScriptableObject
├── PlayerDataServiceInitializer.cs     # MonoBehaviour for scene setup
├── README.md                           # This file
└── VERTICAL_SLICE_PLAN.md             # Implementation plan
```

## Current Status

✅ **Core system complete**
- Interface created
- Local service implemented
- ScriptableObject for data
- Service locator for access
- Scene initializer component
- Presenter updated to use service

⏳ **Next Steps** (when needed)
- Add UI update methods to populate character sheet from data
- Create example character preset ScriptableObjects
- Add data validation
- Add persistence (save/load)

## Testing

The system is designed to be testable:
- Can inject mock service: `PlayerDataServiceLocator.Service = new MockPlayerDataService()`
- All components use interfaces
- No Unity dependencies in core logic (except ScriptableObject)

## Notes

- The system currently uses default `CharacterData` if no ScriptableObject is provided
- UI gets data on-demand (when buttons are clicked)
- Data change events are wired up but UI update methods are TODO (can be added when needed)
- This is a minimal viable system - can be extended as needed

