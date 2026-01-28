# InputSystem Integration Guide

This document explains how to properly integrate Unity's InputSystem with PlayerInput in this project.

## Overview

This project uses Unity's new InputSystem with `PlayerInput` component. Scripts reference input actions via `InputActionReference` and retrieve actual `InputAction` from `PlayerInput` component at runtime.

**Important**: This project uses a polling-based approach for input handling. Input values are checked in `Update()` or `FixedUpdate()` rather than subscribing to events.

## Key Pattern: Getting Input Actions from PlayerInput

### Step 1: Declare InputActionReference Fields

In your script, declare serialized fields for each input action you need:

```csharp
using UnityEngine.InputSystem;

public class YourScript : MonoBehaviour
{
    [SerializeField]
    private InputActionReference yourActionReference;
    
    private InputAction yourAction;
    private PlayerInput playerInput;
}
```

### Step 2: Get PlayerInput Reference

In `Start()`, get the PlayerInput component. There are multiple ways to do this:

**Important**: Use `Start()` instead of `Awake()` or `OnEnable()` because the input singleton gets created in `Awake()`. This ensures the singleton is fully initialized before you try to access it.

#### Option A: Using PlayerInputSingleton

If your project uses a singleton pattern:

```csharp
private void Start()
{
    playerInput = PlayerInputSingleton.Instance?.PlayerInput;
}
```

#### Option B: From Player Controller GameObject

If script is on the same GameObject as PlayerInput (or a parent/child):

```csharp
private void Start()
{
    // Get PlayerInput from the same GameObject
    playerInput = GetComponent<PlayerInput>();
    
    // Or get from parent (if script is on a child)
    if (playerInput == null)
    {
        playerInput = GetComponentInParent<PlayerInput>();
    }
}
```

#### Option C: From Player Reference

If you have a reference to the player GameObject:

```csharp
[SerializeField]
private GameObject player;

private void Start()
{
    if (player != null)
    {
        playerInput = player.GetComponent<PlayerInput>();
    }
}
```

#### Option D: Find in Scene (Last Resort)

If you need to find PlayerInput anywhere in the scene:

```csharp
private void Start()
{
    // Find first PlayerInput in scene
    playerInput = FindFirstObjectByType<PlayerInput>();
    
    if (playerInput == null)
    {
        Debug.LogError("PlayerInput component not found in scene!");
    }
}
```

**Note**: Option D should be used sparingly as it's less efficient and can be fragile if multiple PlayerInputs exist in the scene.

### Step 3: Retrieve InputAction by ID

In `Start()`, retrieve the actual `InputAction` from PlayerInput using the ID from `InputActionReference`:

```csharp
private void Start()
{
    if (playerInput != null)
    {
        // CRITICAL: Use FindAction with the ID from InputActionReference
        yourAction = playerInput.actions.FindAction(yourActionReference.action.id);
    }
}
```

### Step 4: Read Input Values in Update/FixedUpdate

This project uses a polling-based approach. Read input values in `Update()` or `FixedUpdate()`:

```csharp
private void Update()
{
    if (yourAction != null)
    {
        // Check if button was pressed this frame
        if (yourAction.WasPressedThisFrame())
        {
            // Handle button press (e.g., jump, dash, interact)
        }

        // Check if button is currently held down
        if (yourAction.IsPressed())
        {
            // Handle held button (e.g., sprint, charge attack)
        }

        // Check if button was released this frame
        if (yourAction.WasReleasedThisFrame())
        {
            // Handle button release (e.g., stop sprinting, release charge)
        }

        // Read continuous values (movement, look, scroll)
        Vector2 inputValue = yourAction.ReadValue<Vector2>();
        // Use the input value
    }
}
```

## Complete Examples

### Example 1: Button Press Detection

Here's an example for detecting button presses:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class JumpAbility : MonoBehaviour
{
    [SerializeField]
    private InputActionReference jumpActionReference;
    
    private InputAction jumpAction;
    private PlayerInput playerInput;
    
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on this GameObject!");
        }
        else
        {
            // Get action using the ID from InputActionReference
            jumpAction = playerInput.actions.FindAction(jumpActionReference.action.id);
        }
    }
    
    private void Update()
    {
        if (jumpAction != null)
        {
            // Check if jump button was pressed this frame
            if (jumpAction.WasPressedThisFrame())
            {
                // Handle jump
                Debug.Log("Jump pressed!");
            }
        }
    }
}
```

### Example 2: Hold Button Detection

Here's an example for detecting held buttons:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class SprintAbility : MonoBehaviour
{
    [SerializeField]
    private InputActionReference sprintActionReference;
    
    private InputAction sprintAction;
    private PlayerInput playerInput;
    private bool isSprinting;
    
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on this GameObject!");
        }
        else
        {
            // Get action using the ID from InputActionReference
            sprintAction = playerInput.actions.FindAction(sprintActionReference.action.id);
        }
    }
    
    private void Update()
    {
        if (sprintAction != null)
        {
            // Check if sprint button is currently held
            isSprinting = sprintAction.IsPressed();
            
            if (isSprinting)
            {
                // Handle sprinting
            }
        }
    }
}
```

### Example 3: Multiple Actions with Continuous Input

Here's an example combining multiple input types:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private InputActionReference moveActionReference;
    [SerializeField] private InputActionReference jumpActionReference;
    [SerializeField] private InputActionReference interactActionReference;
    
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private PlayerInput playerInput;
    
    private void Start()
    {
        // Get PlayerInput from the same GameObject
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on this GameObject!");
        }
        else
        {
            // Get all actions using their IDs
            moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
            jumpAction = playerInput.actions.FindAction(jumpActionReference.action.id);
            interactAction = playerInput.actions.FindAction(interactActionReference.action.id);
        }
    }
    
    private void Update()
    {
        // Read continuous input (movement)
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            // Process movement
        }
        
        // Check for jump button press
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            // Handle jump
        }
        
        // Check for interact button hold
        if (interactAction != null && interactAction.IsPressed())
        {
            // Handle interact
        }
    }
}
```

## Choosing the Right Approach for Getting PlayerInput

### When to Use PlayerInputSingleton

Use the singleton pattern when:
- Multiple scripts across different GameObjects need access to PlayerInput
- You want a centralized, easy-to-access reference
- Your project has a clear player controller structure

**Example**: Current project uses [`PlayerInputSingleton`](Assets/Scripts/Player/PlayerInputSingleton.cs) for gameplay scripts

### When to Get PlayerInput Directly

Get PlayerInput directly when:
- Your script is on the same GameObject as PlayerInput
- You have a direct reference to the player GameObject
- Your project is simple and doesn't need a singleton
- You want to avoid singleton pattern complexity

**Best practice**: Use `GetComponent<PlayerInput>()` when your script is on the player GameObject.

### Comparison

| Approach | Pros | Cons | Best For |
|----------|------|------|----------|
| **Singleton** | Easy access from anywhere, centralized reference | Adds complexity, can be overkill for simple projects | Large projects with many systems |
| **GetComponent** | Simple, efficient, follows Unity best practices | Only works on same GameObject or hierarchy | Scripts on player GameObject |
| **GetComponentInParent** | Works when script is on child of player | Slightly less efficient | UI or effect scripts on player children |
| **FindFirstObjectByType** | Works anywhere in scene | Slow, fragile if multiple PlayerInputs exist | Last resort, initialization only |

## Why This Pattern?

Using `playerInput.actions.FindAction(inputActionReference.action.id)` ensures that:

1. **Correct Action Map**: You get the action from the currently active action map on the PlayerInput component
2. **Dynamic Switching**: If you switch action maps at runtime (e.g., switching between gameplay and UI controls), the reference automatically resolves to the correct action
3. **Inspector Configuration**: You can assign `InputActionReference` in the Inspector for easy configuration
4. **ID Matching**: Using `action.id` ensures you're getting the exact action referenced, even if action names change
5. **Flexibility**: Works regardless of how you obtain the PlayerInput reference (singleton, GetComponent, etc.)

## Common Mistakes to Avoid

### ❌ Don't use InputActionReference directly for events

```csharp
// WRONG - This won't work correctly with PlayerInput
yourActionReference.action.performed += OnYourActionPerformed;
```

### ❌ Don't use action name string

```csharp
// WRONG - This is fragile and can break if action names change
yourAction = playerInput.actions["Player/YourAction"];
```

### ✅ Use the ID from InputActionReference

```csharp
// CORRECT - Robust and works with dynamic action map switching
yourAction = playerInput.actions.FindAction(yourActionReference.action.id);
```

## Input Polling Methods

This project uses polling-based input methods. Here are the available methods:

- **WasPressedThisFrame()**: Returns `true` if the button was pressed this frame (only once per press)
- **IsPressed()**: Returns `true` while the button is held down
- **WasReleasedThisFrame()**: Returns `true` if the button was released this frame (only once per release)
- **ReadValue<T>()**: Returns the current value of a continuous action (Vector2 for movement/look, float for scroll, etc.)

Use these appropriately:
- Button press: Use `WasPressedThisFrame()` for one-time actions (jump, dash, interact)
- Button hold: Use `IsPressed()` for continuous states (sprint, charge, crouch)
- Button release: Use `WasReleasedThisFrame()` for release detection
- Continuous input: Use `ReadValue<T>()` for movement, look, scroll, and other analog inputs

## Related Files

- [`PlayerInputSingleton.cs`](Assets/Scripts/Player/PlayerInputSingleton.cs) - Singleton for accessing PlayerInput
- [`PlayerMovement.cs`](Assets/Scripts/Player/PlayerMovement.cs:112-136) - Movement input handling example
- [`ObjectManipulator.cs`](Assets/Scripts/Telekinesis/ObjectManipulator.cs:99-123) - Interaction input handling example
- [`RadialMenu.cs`](Assets/Scripts/UI/RadialMenu.cs:127-159) - UI input handling example

## Action Map Switching

For switching between different action maps (e.g., gameplay vs UI), see [`RadialMenu.cs`](Assets/Scripts/UI/RadialMenu.cs:288-348):

```csharp
// Store current action map
previousInputActionAsset = playerInput.actions;

// Switch to UI action map
playerInput.actions = uiInputActionAsset;

// Later, restore previous action map
playerInput.actions = previousInputActionAsset;
```
