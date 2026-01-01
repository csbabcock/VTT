# Player Data System - Vertical Slice Plan

## Goal
Create a simple, working system for the UI to load and display player data. Focus on single-player/local use first, but structure it so networking can be added later without breaking changes.

## Current State
- `InGameUIPresenter` directly creates `CharacterData` with `new CharacterData()`
- Data is hardcoded in the class
- No way to load different characters
- No way to update data and have UI react

## Proposed Solution (Minimal Viable)

### 1. Player Data Service (Simple Interface)
```csharp
public interface IPlayerDataService
{
    // Get current player's data
    CharacterData GetPlayerData();
    
    // Event fired when data changes (UI can subscribe)
    event System.Action<CharacterData> PlayerDataChanged;
    
    // Update data (triggers event)
    void UpdatePlayerData(System.Action<CharacterData> updateAction);
}
```

**Why this approach?**
- Simple interface that UI can depend on
- Event system for reactive UI updates
- Easy to swap implementation later (local → network)

### 2. Local Implementation (For Now)
```csharp
public class LocalPlayerDataService : IPlayerDataService
{
    private CharacterData _playerData;
    public event System.Action<CharacterData> PlayerDataChanged;
    
    public LocalPlayerDataService()
    {
        // Load from ScriptableObject or use defaults
        _playerData = LoadPlayerData();
    }
    
    public CharacterData GetPlayerData() => _playerData;
    
    public void UpdatePlayerData(System.Action<CharacterData> updateAction)
    {
        updateAction(_playerData);
        PlayerDataChanged?.Invoke(_playerData);
    }
    
    private CharacterData LoadPlayerData()
    {
        // Try to load from ScriptableObject, fallback to defaults
        // This allows designers to create character presets
    }
}
```

### 3. ScriptableObject for Data (Designer-Friendly)
```csharp
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerDataAsset : ScriptableObject
{
    public string characterName = "Arlen";
    public int strength = 16;
    public int dexterity = 14;
    // ... other fields
    
    public CharacterData ToCharacterData()
    {
        return new CharacterData
        {
            CharacterName = characterName,
            Strength = strength,
            // ... map fields
        };
    }
}
```

**Benefits:**
- Designers can create character presets in Unity
- No code changes needed to test different characters
- Easy to swap between characters
- Can be saved/loaded from files later

### 4. Service Locator Pattern (Simple Access)
```csharp
public static class PlayerDataServiceLocator
{
    private static IPlayerDataService _service;
    
    public static IPlayerDataService Service
    {
        get
        {
            if (_service == null)
            {
                _service = new LocalPlayerDataService();
            }
            return _service;
        }
        set => _service = value; // Allow injection for testing
    }
}
```

**Why Service Locator?**
- Simple access from anywhere: `PlayerDataServiceLocator.Service.GetPlayerData()`
- Can inject mock for testing
- Can swap to network implementation later
- No dependency injection framework needed yet

### 5. Update InGameUIPresenter
```csharp
private IPlayerDataService _playerDataService;

private void Awake()
{
    // ... existing code ...
    
    // Get service instead of creating directly
    _playerDataService = PlayerDataServiceLocator.Service;
    
    // Subscribe to data changes
    _playerDataService.PlayerDataChanged += OnPlayerDataChanged;
    
    // Load initial data into UI
    UpdateUIFromData(_playerDataService.GetPlayerData());
}

private void OnPlayerDataChanged(CharacterData data)
{
    // Update UI when data changes
    UpdateUIFromData(data);
}

private void UpdateUIFromData(CharacterData data)
{
    // Update character sheet UI elements
    // This is where we'll populate the UI
}
```

## File Structure (Minimal)

```
Assets/Scripts/PlayerData/
├── IPlayerDataService.cs              # Interface
├── LocalPlayerDataService.cs          # Local implementation
├── PlayerDataServiceLocator.cs        # Service access
└── PlayerDataAsset.cs                 # ScriptableObject for data

Assets/Scripts/PlayerData/Data/        # Optional: Character presets
└── (ScriptableObject assets created in Unity)
```

## Implementation Steps

### Step 1: Create Interface
- Simple interface with `GetPlayerData()` and event

### Step 2: Create ScriptableObject
- `PlayerDataAsset` that can be created in Unity
- Maps to `CharacterData`

### Step 3: Create Local Service
- Loads from ScriptableObject (if assigned) or uses defaults
- Implements interface
- Fires events on changes

### Step 4: Create Service Locator
- Simple static access point
- Allows injection for testing

### Step 5: Update Presenter
- Use service instead of direct instantiation
- Subscribe to data changes
- Update UI when data changes

### Step 6: Update View (If Needed)
- Add methods to populate UI from `CharacterData`
- Update character name, ability scores, etc.

## Benefits of This Approach

1. **Simple**: Minimal code, easy to understand
2. **Works Now**: UI can load and display data immediately
3. **Extensible**: Easy to add networking later (just swap implementation)
4. **Testable**: Can inject mock service
5. **Designer-Friendly**: ScriptableObjects can be created in Unity
6. **Reactive**: UI updates automatically when data changes

## Future Extensions (When Needed)

### When Adding Networking:
1. Create `NetworkPlayerDataService : IPlayerDataService`
2. Swap in `PlayerDataServiceLocator.Service = new NetworkPlayerDataService()`
3. UI code doesn't need to change!

### When Adding Persistence:
1. Add `SavePlayerData()` to interface
2. Implement in service
3. Save to file/cloud when needed

### When Adding Multiple Players:
1. Change interface to `GetPlayerData(ulong playerId)`
2. Service manages dictionary of players
3. UI requests specific player

## Migration Path

1. **Phase 1**: Create new system alongside old (no breaking changes)
2. **Phase 2**: Update `InGameUIPresenter` to use service
3. **Phase 3**: Remove direct `CharacterData` instantiation
4. **Phase 4**: Add UI update methods to populate character sheet

This keeps everything working while we migrate!

