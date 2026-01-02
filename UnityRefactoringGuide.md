# Unity Project Refactoring Guide

Use this guide to refactor Unity C# code following SOLID principles and design patterns. Apply these patterns where they provide clear benefit - don't force patterns where simple code works fine.

---

## SOLID Principles

### 1. Single Responsibility Principle (SRP)
Each class should have one reason to change.

**Signs of violation:**
- Classes over 200-300 lines
- Classes with mixed concerns (input + movement + audio + UI)
- Methods doing multiple unrelated things

**Refactoring approach:**
```csharp
// BAD: One class doing everything
public class Player : MonoBehaviour
{
    void Update()
    {
        HandleInput();
        Move();
        PlaySounds();
        UpdateUI();
    }
}

// GOOD: Separate components
[RequireComponent(typeof(PlayerInput), typeof(PlayerMovement), typeof(PlayerAudio))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAudio playerAudio;
}

public class PlayerInput : MonoBehaviour { /* input only */ }
public class PlayerMovement : MonoBehaviour { /* movement only */ }
public class PlayerAudio : MonoBehaviour { /* audio only */ }
```

---

### 2. Open-Closed Principle (OCP)
Classes should be open for extension but closed for modification.

**Signs of violation:**
- Switch statements that grow with new types
- Modifying existing code to add new features
- Large if-else chains based on type

**Refactoring approach:**
```csharp
// BAD: Must modify class to add new shapes
public float GetArea(Shape shape)
{
    switch (shape.type)
    {
        case ShapeType.Rectangle: return shape.width * shape.height;
        case ShapeType.Circle: return shape.radius * shape.radius * Mathf.PI;
        // Must add new cases here...
    }
}

// GOOD: Use abstraction - extend without modifying
public abstract class Shape
{
    public abstract float CalculateArea();
}

public class Rectangle : Shape
{
    public float width, height;
    public override float CalculateArea() => width * height;
}

public class Circle : Shape
{
    public float radius;
    public override float CalculateArea() => radius * radius * Mathf.PI;
}
```

---

### 3. Liskov Substitution Principle (LSP)
Subclasses must be substitutable for their base classes.

**Signs of violation:**
- NotImplementedException in overridden methods
- Empty method overrides
- Subclasses that break parent behavior

**Refactoring approach:**
```csharp
// BAD: Train can't turn, breaks Vehicle contract
public class Vehicle { public virtual void TurnLeft() { } }
public class Train : Vehicle
{
    public override void TurnLeft() => throw new NotImplementedException();
}

// GOOD: Separate interfaces for different capabilities
public interface IMovable
{
    void GoForward();
    void Reverse();
}

public interface ITurnable
{
    void TurnLeft();
    void TurnRight();
}

public class Car : MonoBehaviour, IMovable, ITurnable { /* implements all */ }
public class Train : MonoBehaviour, IMovable { /* only implements movement */ }
```

---

### 4. Interface Segregation Principle (ISP)
Keep interfaces small and focused. Clients shouldn't depend on methods they don't use.

**Signs of violation:**
- Large interfaces with many methods
- Classes implementing interfaces with unused methods
- "God" interfaces

**Refactoring approach:**
```csharp
// BAD: One huge interface
public interface IUnitStats
{
    float Health { get; set; }
    void TakeDamage();
    void Die();
    float MoveSpeed { get; set; }
    void Move();
    int Strength { get; set; }
    void Attack();
}

// GOOD: Split into focused interfaces
public interface IDamageable
{
    float Health { get; set; }
    void TakeDamage();
    void Die();
}

public interface IMovable
{
    float MoveSpeed { get; set; }
    void Move();
}

public interface IAttacker
{
    int Strength { get; set; }
    void Attack();
}

// Compose as needed
public class EnemyUnit : MonoBehaviour, IDamageable, IMovable, IAttacker { }
public class DestructibleProp : MonoBehaviour, IDamageable { }
```

---

### 5. Dependency Inversion Principle (DIP)
High-level modules should not depend on low-level modules. Both should depend on abstractions.

**Signs of violation:**
- Direct references to concrete classes
- Tight coupling between systems
- Hard to swap implementations

**Refactoring approach:**
```csharp
// BAD: Switch directly depends on Door
public class Switch : MonoBehaviour
{
    public Door door;
    public void Toggle() => door.Open();
}

// GOOD: Depend on abstraction
public interface ISwitchable
{
    bool IsActive { get; }
    void Activate();
    void Deactivate();
}

public class Switch : MonoBehaviour
{
    [SerializeField] private MonoBehaviour client; // Assign in Inspector
    private ISwitchable switchable;

    void Start() => switchable = client as ISwitchable;

    public void Toggle()
    {
        if (switchable.IsActive) switchable.Deactivate();
        else switchable.Activate();
    }
}

public class Door : MonoBehaviour, ISwitchable { /* implements interface */ }
public class Light : MonoBehaviour, ISwitchable { /* implements interface */ }
public class Trap : MonoBehaviour, ISwitchable { /* implements interface */ }
```

---

## Design Patterns

### Factory Pattern
Use when: Creating objects with complex setup or when object type is determined at runtime.

```csharp
public interface IProduct
{
    string ProductName { get; set; }
    void Initialize();
}

public abstract class Factory : MonoBehaviour
{
    public abstract IProduct GetProduct(Vector3 position);
}

public class EnemyFactory : Factory
{
    [SerializeField] private Enemy enemyPrefab;

    public override IProduct GetProduct(Vector3 position)
    {
        var instance = Instantiate(enemyPrefab, position, Quaternion.identity);
        instance.Initialize();
        return instance;
    }
}
```

---

### Object Pool Pattern
Use when: Frequently instantiating/destroying objects (bullets, particles, spawned enemies).

```csharp
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile prefab;
    private IObjectPool<Projectile> pool;

    void Awake()
    {
        pool = new ObjectPool<Projectile>(
            createFunc: () => {
                var p = Instantiate(prefab);
                p.Pool = pool;
                return p;
            },
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    public Projectile Get() => pool.Get();
}

public class Projectile : MonoBehaviour
{
    public IObjectPool<Projectile> Pool { get; set; }

    public void Release() => Pool.Release(this);
}
```

---

### Singleton Pattern
Use sparingly for: Game managers, audio managers, global services. Avoid overuse.

```csharp
public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

// Usage
public class GameManager : Singleton<GameManager> { }
```

---

### Command Pattern
Use when: Implementing undo/redo, action queues, input buffering, replay systems.

```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}

public class MoveCommand : ICommand
{
    private Transform target;
    private Vector3 movement;

    public MoveCommand(Transform target, Vector3 movement)
    {
        this.target = target;
        this.movement = movement;
    }

    public void Execute() => target.position += movement;
    public void Undo() => target.position -= movement;
}

public class CommandInvoker
{
    private static Stack<ICommand> undoStack = new Stack<ICommand>();
    private static Stack<ICommand> redoStack = new Stack<ICommand>();

    public static void Execute(ICommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear();
    }

    public static void Undo()
    {
        if (undoStack.Count > 0)
        {
            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
        }
    }

    public static void Redo()
    {
        if (redoStack.Count > 0)
        {
            var command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);
        }
    }
}
```

---

### State Pattern
Use when: Object behavior changes based on internal state (player states, AI states, game states).

```csharp
public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

public class StateMachine
{
    public IState CurrentState { get; private set; }

    public void Initialize(IState startingState)
    {
        CurrentState = startingState;
        startingState.Enter();
    }

    public void TransitionTo(IState nextState)
    {
        CurrentState.Exit();
        CurrentState = nextState;
        nextState.Enter();
    }

    public void Execute() => CurrentState?.Execute();
}

// Example states
public class IdleState : IState
{
    private PlayerController player;

    public IdleState(PlayerController player) => this.player = player;

    public void Enter() => player.PlayAnimation("Idle");
    public void Execute()
    {
        if (player.IsMoving)
            player.StateMachine.TransitionTo(player.WalkState);
    }
    public void Exit() { }
}
```

---

### Observer Pattern (Events)
Use when: Objects need to react to changes without tight coupling (UI updates, achievements, audio triggers).

```csharp
using System;

public class Health : MonoBehaviour
{
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    private int currentHealth;

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
            OnDeath?.Invoke();
    }
}

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health health;

    void OnEnable() => health.OnHealthChanged += UpdateDisplay;
    void OnDisable() => health.OnHealthChanged -= UpdateDisplay;

    private void UpdateDisplay(int newHealth) => /* update UI */;
}
```

---

### Strategy Pattern
Use when: Swapping algorithms/behaviors at runtime (abilities, AI behaviors, movement modes).

```csharp
public abstract class Ability : ScriptableObject
{
    public string abilityName;
    public abstract void Use(GameObject user);
}

[CreateAssetMenu(menuName = "Abilities/Fireball")]
public class FireballAbility : Ability
{
    public float damage = 50f;
    public override void Use(GameObject user) => /* spawn fireball */;
}

[CreateAssetMenu(menuName = "Abilities/Heal")]
public class HealAbility : Ability
{
    public float healAmount = 30f;
    public override void Use(GameObject user) => /* heal logic */;
}

public class AbilityUser : MonoBehaviour
{
    public Ability currentAbility;

    public void UseAbility() => currentAbility?.Use(gameObject);
    public void SetAbility(Ability ability) => currentAbility = ability;
}
```

---

### Flyweight Pattern
Use when: Many objects share common data (units with shared stats, tiles, projectiles).

```csharp
// Shared data (flyweight)
[CreateAssetMenu]
public class UnitData : ScriptableObject
{
    public string unitName;
    public int baseHealth;
    public int baseAttack;
    public int baseDefense;
    public Sprite icon;
}

// Individual instance references shared data
public class Unit : MonoBehaviour
{
    [SerializeField] private UnitData sharedData; // Reference to flyweight

    // Unique per-instance state
    private int currentHealth;
    private Vector3 position;

    void Start() => currentHealth = sharedData.baseHealth;
}
```

---

### MVP Pattern (Model-View-Presenter)
Use when: Building UI systems that need to be testable and maintainable.

```csharp
// Model - data only
[CreateAssetMenu]
public class PlayerData : ScriptableObject
{
    public event Action OnDataChanged;

    [SerializeField] private int health;
    public int Health
    {
        get => health;
        set { health = value; OnDataChanged?.Invoke(); }
    }
}

// View - UI elements (UXML/USS or Unity UI)
// Presenter - mediates between Model and View
public class HealthPresenter : MonoBehaviour
{
    [SerializeField] private PlayerData model;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthText;

    void OnEnable() => model.OnDataChanged += UpdateView;
    void OnDisable() => model.OnDataChanged -= UpdateView;

    private void UpdateView()
    {
        healthBar.value = model.Health / 100f;
        healthText.text = $"{model.Health}/100";
    }
}
```

---

## Refactoring Checklist

When refactoring a class, ask:

1. **Single Responsibility**: Does this class do only one thing? Can I describe its purpose in one sentence?

2. **Dependencies**: Is this class tightly coupled to others? Can I use interfaces instead?

3. **Extensibility**: Will I need to modify this class to add new features? Can I use inheritance/composition instead?

4. **Testability**: Can I test this class in isolation? Are dependencies injectable?

5. **Complexity**: Is there a switch/if-else that will grow? Consider State or Strategy pattern.

6. **Object Creation**: Am I instantiating/destroying objects frequently? Consider Object Pool.

7. **Shared Data**: Do many objects duplicate the same data? Consider Flyweight pattern.

8. **Event Communication**: Are objects directly referencing each other just to notify? Use events/observer.

---

## When NOT to Apply Patterns

- Simple scripts that work fine as-is
- Prototyping phase where flexibility matters more
- Small projects where overhead isn't justified
- When the pattern adds complexity without clear benefit

Remember: **KISS (Keep It Simple, Stupid)**. Only add complexity when it solves a real problem.
