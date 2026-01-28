# Kilo Agent Coding Guidelines

This document provides guidelines and rules for Kilo coding agents working on the GameTools Unity project.

## Core Rules

### 1. Meta Files
**CRITICAL: Do not create `.meta` files.**

Unity automatically generates `.meta` files when you create assets or scripts. Never manually create or edit `.meta` files. If you encounter reference errors due to missing `.meta` files, **ignore them for the current task** - they will be resolved when Unity regenerates them.

**Examples:**
- ❌ Do NOT create: `MyScript.cs.meta`, `MyFolder.meta`, `MyAsset.prefab.meta`
- ✅ Create only: `MyScript.cs`, `MyFolder/`, `MyAsset.prefab`

**Error Handling:**
If you see errors like:
- "The referenced script on this Behaviour is missing!"
- "Missing script component"
- "File meta data is missing"

These are expected when `.meta` files haven't been generated yet. Continue with your task and ignore these errors.

---

## Project Structure

```
GameTools/
├── Assets/                    # Unity assets (scenes, prefabs, etc.)
├── Packages/
│   └── GameTools/            # Main package for game tools
│       ├── Runtime/          # Runtime scripts
│       │   ├── Abilities/    # Ability system components
│       │   └── Controllers/  # Controller implementations
│       └── README.md         # Package documentation
├── ProjectSettings/          # Unity project settings
└── KILO_AGENT_README.md      # This file
```

---

## Coding Conventions

### C# Code Style

**Naming Conventions:**
- **Classes/Structs**: `PascalCase` (e.g., `CharacterControllerPlayer`)
- **Methods**: `PascalCase` (e.g., `GetCurrentSpeed()`)
- **Properties**: `PascalCase` (e.g., `IsGrounded`)
- **Fields**: `camelCase` (e.g., `walkSpeed`, `playerInput`)
- **Private fields**: Prefix with `_` if you prefer (e.g., `_walkSpeed`)
- **Constants**: `PascalCase` (e.g., `MaxSpeed`)
- **Interfaces**: Prefix with `I` (e.g., `IController`)

**File Organization:**
- One class per file
- File name must match class name
- Place files in appropriate directories under `Packages/GameTools/Runtime/`
- Use `AddComponentMenu` attribute for better Unity Inspector organization

**Example:**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameTools.Controllers.Abilities
{
    [AddComponentMenu("Game Tools/Controllers/Abilities/Custom Ability")]
    public class CustomAbility : MonoBehaviour, IControllerAbility
    {
        [SerializeField] private float customValue = 1.0f;
        private IController controller;

        public void Initialize(IController controller)
        {
            this.controller = controller;
        }

        // Implement interface methods...
    }
}
```

---

## Unity-Specific Guidelines

### Input System Integration

This project uses Unity's new Input System. Follow these patterns:

1. **Use InputActionReference for Inspector configuration:**
   ```csharp
   [SerializeField] private InputActionReference moveActionReference;
   ```

2. **Retrieve actions from PlayerInput component:**
   ```csharp
   private PlayerInput playerInput;
   private InputAction moveAction;

   private void Awake()
   {
       playerInput = GetComponent<PlayerInput>();
       moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
   }
   ```

3. **Subscribe to events in OnEnable, unsubscribe in OnDisable:**
   ```csharp
   public void OnEnable()
   {
       moveAction.performed += OnMovePerformed;
       moveAction.canceled += OnMoveCanceled;
   }

   public void OnDisable()
   {
       moveAction.performed -= OnMovePerformed;
       moveAction.canceled -= OnMoveCanceled;
   }
   ```

### Component References

- Use `[SerializeField]` for Unity component references
- Cache references in `Awake()` or `Initialize()` methods
- Use `GetComponent<T>()` for required components
- Use `TryGetComponent<T>()` for optional components

**Example:**
```csharp
private CharacterController characterController;
private Animator animator;

private void Awake()
{
    characterController = GetComponent<CharacterController>();
    TryGetComponent(out animator);
}
```

### Physics and Updates

- Use `Update()` for frame-dependent logic (input, animation)
- Use `FixedUpdate()` for physics calculations
- Use `Time.deltaTime` in `Update()`
- Use `Time.fixedDeltaTime` in `FixedUpdate()`

**Example:**
```csharp
public void Update()
{
    HandleInput();
    UpdateAnimations();
}

public void FixedUpdate()
{
    HandleMovement();
    HandlePhysics();
}
```

### Unity Object API (Unity 6000.0+)

Unity 6000.0+ introduced a new Object API with improved methods for finding objects. Use these new methods instead of the deprecated ones.

**New Object Finding Methods:**

```csharp
// Find all objects of type T in the scene
T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

// Find first object of type T
T obj = FindFirstObjectByType<T>();

// Find objects with specific sorting
T[] sortedObjects = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
```

**FindObjectsInactive Options:**
- `FindObjectsInactive.Exclude` - Only active objects (default)
- `FindObjectsInactive.Include` - Include inactive objects

**FindObjectsSortMode Options:**
- `FindObjectsSortMode.None` - No specific order (fastest)
- `FindObjectsSortMode.InstanceID` - Sort by instance ID
- `FindObjectsSortMode.None` - Use when order doesn't matter for performance

**Deprecated Methods (Do NOT use):**
- ❌ `Object.FindObjectsOfType<T>()` - Deprecated
- ❌ `Object.FindObjectOfType<T>()` - Deprecated
- ❌ `GameObject.FindObjectsOfType<T>()` - Deprecated

**Examples:**

```csharp
// Find all active players in the scene
PlayerController[] players = FindObjectsByType<PlayerController>(
    FindObjectsInactive.Exclude,
    FindObjectsSortMode.None
);

// Find the main camera (first one found)
Camera mainCamera = FindFirstObjectByType<Camera>();

// Find all audio sources including inactive ones
AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(
    FindObjectsInactive.Include,
    FindObjectsSortMode.None
);

// Find all enemies sorted by instance ID
Enemy[] enemies = FindObjectsByType<Enemy>(
    FindObjectsInactive.Exclude,
    FindObjectsSortMode.InstanceID
);
```

**Performance Notes:**
- Use `FindObjectsSortMode.None` when order doesn't matter (best performance)
- Cache results when possible instead of calling repeatedly
- Consider using object references or singletons for frequently accessed objects

**Reference:** [Unity Object API Documentation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html)

### Unity Object Null Checking

**CRITICAL: Do NOT use null propagation (?.) with Unity objects.**

Unity overrides the `==` operator for objects derived from `UnityEngine.Object` to check the validity of the underlying native C++ object, not just the C# reference. This means:

- A C# object reference can be non-null even if the underlying Unity object is destroyed
- The `==` operator returns `true` when compared to `null` for destroyed objects
- Null propagation operators (`?.`, `??`) do NOT use this custom equality logic
- They perform a direct C# null check, which can lead to unexpected behavior

**The Problem:**

```csharp
// ❌ WRONG - Using null propagation with Unity objects
public void ExampleWrong(GameObject foo)
{
    Object.Destroy(foo);
    
    // foo is not null in C#, but represents a destroyed Unity object
    foo?.SetActive(true);  // This will execute and cause an exception!
    
    // The null coalescing operator also fails
    GameObject result = foo ?? new GameObject();  // Uses foo, not new GameObject!
}

// ✅ CORRECT - Use explicit null checks
public void ExampleCorrect(GameObject foo)
{
    Object.Destroy(foo);
    
    // Unity's custom == operator correctly identifies destroyed objects
    if (foo != null)
    {
        foo.SetActive(true);
    }
}

// ✅ CORRECT - Use Unity's null check utilities
public void ExampleWithUnityCheck(GameObject foo)
{
    Object.Destroy(foo);
    
    // Explicit check using Unity's custom equality
    if (foo == null)
    {
        Debug.Log("Object was destroyed");
    }
    else
    {
        foo.SetActive(true);
    }
}
```

**Real-World Example:**

```csharp
// ❌ WRONG - This will cause issues
public class Health : MonoBehaviour
{
    public GameObject deathEffect;

    public void Die()
    {
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            Object.Destroy(deathEffect);
        }
        
        // Later in code...
        deathEffect?.SetActive(false);  // ❌ Will execute on destroyed object!
    }
}

// ✅ CORRECT - Proper null checking
public class Health : MonoBehaviour
{
    public GameObject deathEffect;

    public void Die()
    {
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            Object.Destroy(deathEffect);
        }
        
        // Later in code...
        if (deathEffect != null)  // ✅ Uses Unity's custom == operator
        {
            deathEffect.SetActive(false);
        }
    }
}
```

**Common Patterns to Avoid:**

```csharp
// ❌ AVOID - Null propagation with Unity objects
myComponent?.MyMethod();
myTransform?.position = Vector3.zero;
myRigidbody?.AddForce(force);

// ❌ AVOID - Null coalescing with Unity objects
GameObject obj = myGameObject ?? new GameObject();
Transform t = myTransform ?? transform;

// ❌ AVOID - Null conditional in LINQ
var activeObjects = objects.Where(o => o?.activeSelf == true);

// ✅ CORRECT - Explicit null checks
if (myComponent != null)
{
    myComponent.MyMethod();
}

if (myTransform != null)
{
    myTransform.position = Vector3.zero;
}

if (myRigidbody != null)
{
    myRigidbody.AddForce(force);
}

// ✅ CORRECT - Explicit null checks for coalescing
GameObject obj = (myGameObject != null) ? myGameObject : new GameObject();
Transform t = (myTransform != null) ? myTransform : transform;

// ✅ CORRECT - Explicit null checks in LINQ
var activeObjects = objects.Where(o => o != null && o.activeSelf);
```

**Best Practices:**

1. **Always use explicit null checks** for Unity objects:
   ```csharp
   if (unityObject != null) { /* use object */ }
   ```

2. **Use `TryGetComponent`** for optional components:
   ```csharp
   if (TryGetComponent(out Animator animator))
   {
       animator.SetBool("IsRunning", true);
   }
   ```

3. **Cache references** and check them once:
   ```csharp
   private Rigidbody cachedRigidbody;
   
   private void Awake()
   {
       cachedRigidbody = GetComponent<Rigidbody>();
   }
   
   private void FixedUpdate()
   {
       if (cachedRigidbody != null)
       {
           cachedRigidbody.AddForce(force);
       }
   }
   ```

4. **Use `Object.DestroyImmediate`** only in Editor code, never in runtime

5. **For collections**, filter out nulls explicitly:
   ```csharp
   // ❌ WRONG
   var validObjects = objects.Where(o => o != null);
   
   // ✅ CORRECT - Unity's null check handles destroyed objects
   var validObjects = objects.Where(o => o != null).ToList();
   ```

**Summary:**
- Never use `?.` or `??` with `UnityEngine.Object` derivatives
- Always use explicit `!= null` checks
- Unity's custom `==` operator handles destroyed objects correctly
- Null propagation operators only check C# reference, not Unity object validity

---

## Ability System Guidelines

### Creating New Abilities

All abilities must implement the [`IControllerAbility`](Packages/GameTools/Runtime/Abilities/IControllerAbility.cs) interface.

**Required Methods:**
1. `Initialize(IController controller)` - Setup ability with controller reference
2. `OnEnable()` - Subscribe to input events
3. `OnDisable()` - Unsubscribe from input events
4. `Update()` - Frame-based updates
5. `FixedUpdate()` - Physics-based updates
6. `GetVelocityModification()` - Return velocity to add to movement
7. `IsActive()` - Return whether ability is active
8. `GetPriority()` - Return priority for ability conflicts

**Ability Template:**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using GameTools.Controllers.Abilities;

[AddComponentMenu("Game Tools/Controllers/Abilities/Your Ability")]
public class YourAbility : MonoBehaviour, IControllerAbility
{
    [Header("Configuration")]
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private float abilityValue = 1.0f;

    private IController controller;
    private PlayerInput playerInput;
    private InputAction abilityAction;
    private bool isActive;

    public void Initialize(IController controller)
    {
        this.controller = controller;
        playerInput = GetComponent<PlayerInput>();
    }

    public void OnEnable()
    {
        if (playerInput != null && actionReference != null)
        {
            abilityAction = playerInput.actions.FindAction(actionReference.action.id);
            abilityAction.performed += OnAbilityPerformed;
            abilityAction.canceled += OnAbilityCanceled;
        }
    }

    public void OnDisable()
    {
        if (abilityAction != null)
        {
            abilityAction.performed -= OnAbilityPerformed;
            abilityAction.canceled -= OnAbilityCanceled;
        }
    }

    private void OnAbilityPerformed(InputAction.CallbackContext context)
    {
        isActive = true;
    }

    private void OnAbilityCanceled(InputAction.CallbackContext context)
    {
        isActive = false;
    }

    public void Update() { }

    public void FixedUpdate() { }

    public Vector3 GetVelocityModification()
    {
        return isActive ? CalculateVelocity() : Vector3.zero;
    }

    public bool IsActive() => isActive;

    public int GetPriority() => 0;

    private Vector3 CalculateVelocity()
    {
        // Calculate and return velocity modification
        return Vector3.zero;
    }
}
```

### Ability Priority System

- Higher priority abilities override lower priority ones
- Use priority values like: 10 (dash), 5 (jump), 0 (sprint)
- The controller applies velocity from the highest priority active ability

---

## Controller Interface Guidelines

All controllers must implement the [`IController`](Packages/GameTools/Runtime/Abilities/IController.cs) interface.

**Required Properties:**
- `Transform` - Controller's transform
- `Velocity` - Current velocity
- `IsGrounded` - Grounded state
- `GameObject` - Attached GameObject

**Required Methods:**
- `Move(Vector3 movement)` - Move the controller
- `SetPosition(Vector3 position)` - Set position
- `GetPosition()` - Get current position
- `GetRotation()` - Get rotation
- `SetRotation(Quaternion rotation)` - Set rotation
- `CanActivateAbilities()` - Check if abilities can activate

---

## Documentation Guidelines

### Code Documentation

- Use XML documentation comments for public APIs
- Include `<summary>` for all public classes and methods
- Include `<param>` tags for parameters
- Include `<returns>` tags for return values

**Example:**
```csharp
/// <summary>
/// Gets the current movement speed of the character.
/// </summary>
/// <returns>The current movement speed.</returns>
public float GetCurrentSpeed()
{
    return currentSpeed;
}
```

### README Updates

When adding new features:
1. Update the feature checklist in [`Packages/GameTools/README.md`](Packages/GameTools/README.md)
2. Add documentation for new components
3. Include setup instructions
4. Provide usage examples
5. Update version history

---

## Testing Guidelines

### Manual Testing

After making changes:
1. Test in Unity Editor
2. Verify no console errors
3. Test all input mappings
4. Verify ability interactions
5. Check animation integration (if applicable)

### Known Issues to Ignore

- Missing `.meta` file references (will be auto-generated)
- Initial script compilation errors (wait for Unity to recompile)
- "Missing script" warnings on prefabs (meta file issue)

---

## Common Patterns

### Singleton Pattern (Use Sparingly)

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
```

### Event Pattern

```csharp
public class Health : MonoBehaviour
{
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }
}
```

### State Pattern (For Complex Abilities)

```csharp
public enum AbilityState
{
    Idle,
    Active,
    Cooldown,
    Disabled
}

public class ComplexAbility : MonoBehaviour, IControllerAbility
{
    private AbilityState currentState;

    public void Update()
    {
        switch (currentState)
        {
            case AbilityState.Idle:
                HandleIdle();
                break;
            case AbilityState.Active:
                HandleActive();
                break;
            case AbilityState.Cooldown:
                HandleCooldown();
                break;
        }
    }

    private void HandleIdle() { }
    private void HandleActive() { }
    private void HandleCooldown() { }
}
```

---

## File Naming and Organization

### Script Files

- Use PascalCase for file names
- Match file name exactly to class name
- Place in appropriate namespace folder

**Examples:**
- `CharacterControllerPlayer.cs` → `GameTools.Controllers`
- `SprintingAbility.cs` → `GameTools.Controllers.Abilities`
- `IController.cs` → `GameTools.Controllers.Abilities`

### Folder Structure

```
Packages/GameTools/Runtime/
├── Controllers/           # Controller implementations
│   ├── CharacterControllerPlayer.cs
│   └── RigidbodyController.cs (future)
└── Abilities/            # Ability components
    ├── IController.cs
    ├── IControllerAbility.cs
    ├── SprintingAbility.cs
    ├── JumpingAbility.cs
    └── DashingAbility.cs
```

---

## Performance Guidelines

### Optimization Tips

1. **Cache component references** in `Awake()` or `Initialize()`
2. **Avoid `GetComponent<T>()`** in `Update()` or `FixedUpdate()`
3. **Use object pooling** for frequently spawned objects
4. **Minimize allocations** in hot paths (Update, FixedUpdate)
5. **Use `StringBuilder`** for complex string concatenation

### Physics Optimization

- Use `FixedUpdate()` for physics calculations
- Set appropriate fixed timestep in Project Settings
- Use layers and collision matrix wisely
- Avoid expensive collision checks in Update()

---

## Debugging Guidelines

### Debug Logging

Use `Debug.Log()` with meaningful messages:
```csharp
Debug.Log($"[SprintingAbility] Sprint activated with multiplier: {speedMultiplier}");
```

### Debug Visualization

Use `Gizmos` for visual debugging:
```csharp
private void OnDrawGizmos()
{
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, groundCheckDistance);
}
```

---

## Version Control Notes

### What to Commit

- ✅ Source code files (.cs)
- ✅ Scene files (.unity)
- ✅ Prefab files (.prefab)
- ✅ Asset files (textures, models, audio)
- ✅ Package manifests (package.json)
- ✅ README files

### What NOT to Commit

- ❌ `.meta` files (Unity generates these)
- ❌ Library folder (Unity cache)
- ❌ Temp folder
- ❌ User-specific settings
- ❌ Generated assemblies

---

## Quick Reference

### Common Unity Methods

```csharp
// Lifecycle
Awake()           // Called when script instance is loaded
Start()           // Called before first Update
OnEnable()        // Called when object becomes active
OnDisable()       // Called when object becomes inactive
Update()          // Called every frame
FixedUpdate()     // Called every physics frame
LateUpdate()      // Called after all Update calls
OnDestroy()       // Called when object is destroyed

// Input
OnMouseDown()     // Mouse button pressed
OnMouseUp()       // Mouse button released
OnCollisionEnter() // Collision started
OnCollisionExit()  // Collision ended
OnTriggerEnter()  // Trigger entered
OnTriggerExit()   // Trigger exited
```

### Common Unity Components

```csharp
// Get components
GetComponent<T>()
TryGetComponent<T>(out T component)
GetComponentsInChildren<T>()

// Transform operations
transform.position
transform.rotation
transform.localScale
transform.Translate()
transform.Rotate()
transform.LookAt()

// Physics
Rigidbody.AddForce()
Rigidbody.velocity
CharacterController.Move()
CharacterController.isGrounded
```

---

## Getting Help

### Documentation

- Unity Documentation: https://docs.unity3d.com/
- Input System Package: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- Project README: [`Packages/GameTools/README.md`](Packages/GameTools/README.md)

### Project-Specific

- See existing abilities for patterns
- Check [`IControllerAbility`](Packages/GameTools/Runtime/Abilities/IControllerAbility.cs) for interface requirements
- Review [`CharacterControllerPlayer`](Packages/GameTools/Runtime/CharacterControllerPlayer.cs) for controller implementation

---

## Summary Checklist

Before submitting work:

- [ ] No `.meta` files were created manually
- [ ] All code follows naming conventions
- [ ] Public APIs have XML documentation
- [ ] Input events are properly subscribed/unsubscribed
- [ ] Component references are cached
- [ ] Code compiles without errors
- [ ] README is updated if new features were added
- [ ] Version history is updated
- [ ] Ignore any meta-file related errors
- [ ] Used new Object API methods (FindObjectsByType, FindFirstObjectByType)
- [ ] No null propagation (?.) used with Unity objects
- [ ] All Unity object null checks use explicit `!= null` comparisons

---

**Remember: The most important rule is to never manually create `.meta` files. Unity will handle them automatically.**
