# Child of Eclipse - Game Documentation

## Overview

Child of Eclipse is a Unity 3D game built with HDRP (High Definition Render Pipeline) that features a unique solar state mechanic, physics-based player movement, and modular interaction systems. The game uses Unity's new Input System and Cinemachine 3 for camera control.

## Table of Contents

- [Project Structure](#project-structure)
- [Core Systems](#core-systems)
  - [Player System](#player-system)
  - [Input System](#input-system)
  - [Interaction System](#interaction-system)
  - [Health System](#health-system)
  - [Solar State System](#solar-state-system)
  - [Trigger System](#trigger-system)
  - [Teleporter System](#teleporter-system)
  - [Camera System](#camera-system)
- [Gameplay Mechanics](#gameplay-mechanics)
- [Setup Guide](#setup-guide)
- [Related Documentation](#related-documentation)

---

## Project Structure

```
ChildOfEclipse/
├── Assets/
│   ├── Scripts/
│   │   ├── Player/           # Player control and input
│   │   ├── Interaction/      # Interaction mechanics
│   │   ├── Health/           # Health, death, and respawn
│   │   └── World/           # World systems (triggers, solar state, teleporters)
│   ├── InputActions/         # Input action assets
│   ├── Materials/            # Game materials
│   ├── Prefabs/             # Prefabricated objects
│   ├── Scenes/              # Game scenes
│   ├── Shaders/             # Custom shaders
│   └── UI/                 # UI elements and Toolkit
├── Packages/                # Unity packages
├── ProjectSettings/          # Unity project settings
└── README.md               # This file
```

---

## Core Systems

### Player System

The player system handles character movement, physics, and state management using a Rigidbody-based controller.

#### Components

**[`RigidbodyPlayerController`](Assets/Scripts/Player/RigidbodyPlayerController.cs)**
- Physics-based character controller with full movement capabilities
- Supports walking, sprinting, crouching, and jumping
- Camera-relative movement with slope handling
- Coyote time and jump buffering for responsive controls
- Variable jump height based on button hold duration

**Key Features:**
- **Movement**: Smooth acceleration/deceleration with configurable speeds
- **Sprint**: Speed multiplier when sprinting (default: 1.8x)
- **Crouch**: Reduces collider height and movement speed (default: 0.5x)
- **Jump**: Variable height with coyote time (0.1s) and jump buffer (0.1s)
- **Ground Detection**: Sphere cast-based ground checking with slope support
- **Rotation**: Smooth rotation towards movement direction

**Configuration:**
```csharp
[Header("Movement Settings")]
[SerializeField] private float walkSpeed = 5f;
[SerializeField] private float sprintSpeedMultiplier = 1.8f;
[SerializeField] private float crouchSpeedMultiplier = 0.5f;
[SerializeField] private float acceleration = 10f;
[SerializeField] private float deceleration = 10f;
[SerializeField] private float maxSlopeAngle = 45f;
[SerializeField] private float rotationSpeed = 10f;

[Header("Jump Settings")]
[SerializeField] private float jumpForce = 8f;
[SerializeField] private float jumpHoldTime = 0.2f;
[SerializeField] private float fallGravityMultiplier = 1.5f;
[SerializeField] private float coyoteTime = 0.1f;
[SerializeField] private float jumpBufferTime = 0.1f;
```

**Public Properties:**
- `IsGrounded`: Returns true if player is on ground
- `IsSprinting`: Returns true if player is sprinting
- `IsCrouching`: Returns true if player is crouching
- `CurrentSpeed`: Returns current movement speed

**Public Methods:**
- `ForceJump()`: Forces the player to jump
- `SetCrouch(bool)`: Sets crouch state externally
- `AddExternalForce(Vector3, ForceMode)`: Applies external force
- `Teleport(Vector3)`: Teleports player to position

---

### Input System

The game uses Unity's new Input System with a singleton pattern for centralized input access.

#### Components

**[`PlayerInputSingleton`](Assets/Scripts/Player/PlayerInputSingleton.cs)**
- Singleton providing access to PlayerInput component
- Created in Awake() to ensure availability in Start()
- Prevents duplicate instances

**Usage Pattern:**
```csharp
// Get PlayerInput from singleton
var playerInput = PlayerInputSingleton.Instance?.PlayerInput;

// Retrieve InputAction using ID from InputActionReference
InputAction moveAction = playerInput.actions.FindAction(moveActionReference.action.id);

// Read input values in Update/FixedUpdate
Vector2 moveInput = moveAction.ReadValue<Vector2>();
if (jumpAction.WasPressedThisFrame()) { /* Handle jump */ }
if (sprintAction.IsPressed()) { /* Handle sprint */ }
```

**Important Notes:**
- Use `Start()` instead of `Awake()` for input initialization
- Retrieve actions using `FindAction(action.id)` for robustness
- Use polling-based input (WasPressedThisFrame, IsPressed, WasReleasedThisFrame)
- See [`INPUTSYSTEM_README.md`](INPUTSYSTEM_README.md) for detailed documentation

---

### Interaction System

The interaction system allows players to interact with objects in the 3D world using raycasting from the camera.

#### Components

**[`IInteractable`](Assets/Scripts/Interaction/IInteractable.cs)**
- Interface for objects that can be interacted with
- Must be implemented on any interactable GameObject

**Interface Methods:**
```csharp
void OnInteract(GameObject interactor, RaycastHit hitInfo);
void OnHoverEnter(GameObject interactor);
void OnHoverExit(GameObject interactor);
bool CanInteract { get; }
string InteractionDescription { get; }
```

**[`InteractPointer`](Assets/Scripts/Interaction/InteractPointer.cs)**
- Handles pointer-based interaction with 3D objects
- Uses raycasting from camera to detect interactables
- Supports world-space UI at interaction point
- Line-of-sight checking for realistic interaction

**Features:**
- Raycast from camera through pointer position
- Configurable max ray distance and layer mask
- Optional line-of-sight requirement
- World UI prefab spawning at hit point
- Hover enter/exit callbacks
- Click interaction detection

**Configuration:**
```csharp
[Header("Raycast Settings")]
[SerializeField] private float maxRayDistance = 100f;
[SerializeField] private LayerMask interactableLayerMask = 1;
[SerializeField] private bool requireLineOfSight = true;

[Header("World UI Settings")]
[SerializeField] private GameObject worldUIPrefab;
[SerializeField] private float surfaceOffset = 0.1f;
[SerializeField] private bool hideUIWhenNoHit = true;
```

**[`SolarStateSwapInteractable`](Assets/Scripts/Interaction/SolarStateSwapInteractable.cs)**
- Interactable that swaps player's SolarState with object's state
- Player must have SolarState component
- Only swaps when states are different

**Features:**
- Visual feedback on hover (color change)
- Custom interaction descriptions
- Particle and sound effects on interaction
- Automatic state tracking and validation
- Debug visualization in editor

**Interaction Flow:**
1. Player hovers over object → Object highlights
2. Player clicks → States swap between player and object
3. Visual and audio effects play
4. Material updates via SolarStateMaterial

---

### Health System

The health system manages damage, death, and respawning for game entities.

#### Components

**[`HealthComponent`](Assets/Scripts/Health/HealthComponent.cs)**
- Manages health for any game object
- Handles damage, healing, and death
- Configurable death behavior (destroy/disable)

**Features:**
- Configurable max health and current health
- Damage and healing with clamping
- Death events with optional destroy/disable
- UnityEvents for integration with other systems
- Revive functionality

**Configuration:**
```csharp
[Header("Health Settings")]
[SerializeField] private float maxHealth = 100f;
[SerializeField] private bool destroyOnDeath = true;
[SerializeField] private float destroyDelay = 0f;
[SerializeField] private bool disableOnDeath = false;

[Header("Events")]
public UnityEvent<float> OnDamageTaken;
public UnityEvent<float> OnHealed;
public UnityEvent OnDeath;
public UnityEvent<float> OnHealthChanged;
```

**Public Properties:**
- `CurrentHealth`: Current health value
- `MaxHealth`: Maximum health value
- `IsAlive`: True if health > 0
- `IsDead`: True if health <= 0
- `HealthPercentage`: Health as 0-1 value

**Public Methods:**
- `TakeDamage(float)`: Apply damage
- `Heal(float)`: Restore health
- `SetHealth(float)`: Set health directly
- `SetMaxHealth(float, bool)`: Change max health
- `Kill()`: Instant death
- `Revive()`: Restore to full health
- `ResetHealth()`: Reset to max health

**[`KillZone`](Assets/Scripts/Health/KillZone.cs)**
- Trigger zone that instantly kills entities with HealthComponent
- Useful for lava, bottomless pits, etc.

**Features:**
- Tag and layer filtering
- One-time kill option
- Kill delay for dramatic effect
- Events for kill tracking
- Visual gizmos in editor

**Configuration:**
```csharp
[Header("Kill Zone Settings")]
[SerializeField] private string[] targetTags;
[SerializeField] private LayerMask targetLayers = -1;
[SerializeField] private bool oneTimeKill = false;
[SerializeField] private float killDelay = 0f;

[Header("Events")]
public UnityEvent<GameObject> OnObjectEntered;
public UnityEvent<GameObject> OnObjectKilled;
```

**[`Checkpoint`](Assets/Scripts/Health/Checkpoint.cs)**
- Respawn point that activates when player enters
- Sets spawn point for RespawnableComponent

**Features:**
- One-time or reusable checkpoints
- Visual feedback (color change, light, particles)
- Automatic activation on entry
- Deactivates other checkpoints when activated
- Respawn offset configuration

**Configuration:**
```csharp
[Header("Checkpoint Settings")]
[SerializeField] private bool oneTimeUse = false;
[SerializeField] private bool autoActivate = true;
[SerializeField] private string activatorTag = "Player";
[SerializeField] private Vector3 respawnOffset = Vector3.zero;

[Header("Visual Feedback")]
[SerializeField] private Renderer checkpointRenderer;
[SerializeField] private Color inactiveColor = Color.red;
[SerializeField] private Color activeColor = Color.green;
[SerializeField] private Light checkpointLight;
[SerializeField] private ParticleSystem activationParticles;
```

**[`RespawnableComponent`](Assets/Scripts/Health/RespawnableComponent.cs)**
- Manages respawning for entities with health
- Supports multiple respawn locations

**Respawn Locations:**
- `OriginalPosition`: Spawn at initial position
- `LastCheckpoint`: Spawn at last activated checkpoint
- `CustomSpawnPoint`: Spawn at custom transform

**Features:**
- Configurable respawn delay
- Optional disable during delay
- Full health restoration
- Velocity reset
- Max respawn count limit
- UnityEvents for respawn lifecycle

**Configuration:**
```csharp
[Header("Respawn Settings")]
[SerializeField] private RespawnLocation respawnLocation;
[SerializeField] private Transform customSpawnPoint;
[SerializeField] private float respawnDelay = 2f;
[SerializeField] private bool disableDuringDelay = true;
[SerializeField] private bool restoreFullHealth = true;
[SerializeField] private bool resetVelocity = true;
[SerializeField] private int maxRespawnCount = -1;
```

**Public Methods:**
- `SetCheckpointPosition(Vector3)`: Set checkpoint spawn point
- `ClearCheckpoint()`: Clear checkpoint
- `Respawn()`: Manually trigger respawn
- `RespawnAt(Vector3, Quaternion)`: Respawn at specific position
- `ResetRespawnCount()`: Reset respawn counter

---

### Solar State System

The solar state system is a unique mechanic allowing objects and the player to exist in different states (Sun, Moon, Eclipse), each with distinct visual and gameplay properties.

#### Components

**[`SolarState`](Assets/Scripts/World/SolarState.cs)**
- Core component managing solar state for individual objects
- Three states: Sun, Moon, Eclipse
- Event-based state change notifications

**States:**
```csharp
public enum SolarStateValue
{
    Sun,
    Moon,
    Eclipse
}
```

**Features:**
- Independent state per object
- Event firing on state change
- State checking methods
- Configurable initial state

**Public Properties:**
- `CurrentState`: Gets or sets current state

**Public Methods:**
- `SetSunState()`: Change to Sun state
- `SetMoonState()`: Change to Moon state
- `SetEclipseState()`: Change to Eclipse state
- `IsSunState()`: Check if in Sun state
- `IsMoonState()`: Check if in Moon state
- `IsEclipseState()`: Check if in Eclipse state

**Events:**
```csharp
public event Action<SolarStateValue, SolarStateValue> OnSolarStateChanged;
// Parameters: oldState, newState
```

**[`SolarStateMaterial`](Assets/Scripts/World/SolarStateMaterial.cs)**
- Changes material based on solar state
- Automatically updates when state changes
- Supports multiple renderers

**Features:**
- Separate materials for each state
- Automatic material switching on state change
- Support for multiple renderers
- Runtime material assignment

**Configuration:**
```csharp
[Header("Materials")]
[SerializeField] private Material _sunMaterial;
[SerializeField] private Material _moonMaterial;
[SerializeField] private Material _eclipseMaterial;

[Header("Renderer Settings")]
[SerializeField] private Renderer[] _targetRenderers;
```

**Public Methods:**
- `GetMaterialForState(SolarStateValue)`: Get material for state
- `SetMaterialForState(SolarStateValue, Material)`: Set material for state

---

### Trigger System

The trigger system provides a flexible, component-based way to create interactive zones that activate when objects enter and meet specific conditions.

#### Components

**[`GenericTrigger`](Assets/Scripts/World/GenericTrigger.cs)**
- Generic overlap box that activates when objects are inside
- Requires all trigger options to pass for activation
- Supports multiple trigger reactions

**Features:**
- Configurable overlap box size and position
- Layer mask filtering
- Trigger once per entry option
- Cooldown system
- Reset state on exit
- Visual gizmos in editor

**Configuration:**
```csharp
[Header("Overlap Box Settings")]
[SerializeField] private Vector3 overlapBoxSize = new Vector3(3f, 3f, 3f);
[SerializeField] private Vector3 overlapBoxCenter = Vector3.zero;

[Header("Trigger Settings")]
[SerializeField] private LayerMask triggerLayers = -1;
[SerializeField] private bool triggerOncePerEntry = true;
[SerializeField] private float triggerCooldown = 0.5f;
[SerializeField] private bool resetStateOnExit = true;
```

**[`ITriggerOption`](Assets/Scripts/World/ITriggerOption.cs)**
- Interface for conditions that must pass for trigger activation
- Multiple options can be added to a single trigger

**Interface Methods:**
```csharp
bool ShouldActivate(GameObject triggeringObject, GenericTrigger trigger);
void OnActivated(GameObject triggeringObject, GenericTrigger trigger);
```

**Available Options:**
- [`SolarStateTriggerOption`](Assets/Scripts/World/SolarStateTriggerOption.cs): Activates only when object has specific solar state
- [`NavAgentStoppedTriggerOption`](Assets/Scripts/World/NavAgentStoppedTriggerOption.cs): Activates when NavMesh agent stops

**[`ITriggerReaction`](Assets/Scripts/World/ITriggerReaction.cs)**
- Interface for actions that occur when trigger activates
- Multiple reactions can be added to a single trigger

**Interface Methods:**
```csharp
void OnActivated(GameObject triggeringObject, GenericTrigger trigger);
void OnReset(GameObject triggeringObject, GenericTrigger trigger);
```

**Available Reactions:**
- [`TriggerReactionGameObjectToggle`](Assets/Scripts/World/TriggerReactionGameObjectToggle.cs): Toggles GameObjects on activation

**[`TriggerReactionGameObjectToggle`](Assets/Scripts/World/TriggerReactionGameObjectToggle.cs)**
- Toggles GameObjects when trigger activates
- Supports enable, disable, or toggle modes

**Configuration:**
```csharp
[Header("GameObject Toggle Settings")]
[SerializeField] private List<GameObject> objectsToEnable;
[SerializeField] private List<GameObject> objectsToDisable;
[SerializeField] private bool toggleState = false;
[SerializeField] private List<GameObject> objectsToToggle;
```

**Usage Example:**
```csharp
// Create a trigger that only activates when player is in Sun state
// and enables a platform when triggered
GameObject triggerObject = new GameObject("SunStateTrigger");
triggerObject.AddComponent<GenericTrigger>();

var solarOption = triggerObject.AddComponent<SolarStateTriggerOption>();
solarOption.RequiredState = SolarStateValue.Sun;

var toggleReaction = triggerObject.AddComponent<TriggerReactionGameObjectToggle>();
toggleReaction.objectsToEnable.Add(platformObject);
```

---

### Teleporter System

The teleporter system allows instant transportation of objects between locations.

#### Components

**[`Teleporter`](Assets/Scripts/World/Teleporter.cs)**
- Teleports objects from entry box to destination
- Configurable behavior for rotation and velocity

**Features:**
- Configurable entry box size and position
- Destination transform reference
- Optional preserve of rotation/velocity
- Layer mask filtering
- Cooldown system
- Visual gizmos showing entry and destination

**Configuration:**
```csharp
[Header("Entry Box Settings")]
[SerializeField] private Vector3 entryBoxSize = new Vector3(3f, 3f, 3f);
[SerializeField] private Vector3 entryBoxCenter = Vector3.zero;

[Header("Destination Settings")]
[SerializeField] private Transform destination;
[SerializeField] private Vector3 destinationOffset = Vector3.zero;
[SerializeField] private bool preserveRotation = true;
[SerializeField] private bool preserveVelocity = false;
[SerializeField] private bool preserveAngularVelocity = false;

[Header("Detection Settings")]
[SerializeField] private LayerMask teleportLayers = -1;
[SerializeField] private float teleportCooldown = 0.5f;
[SerializeField] private bool teleportOncePerEntry = true;
```

**Usage:**
1. Place Teleporter at entry location
2. Create empty GameObject at destination
3. Assign destination transform to Teleporter
4. Configure entry box size and layers
5. Objects entering entry box will teleport to destination

---

### Camera System

The game uses Cinemachine 3 for camera control.

**Key Points:**
- Uses `Unity.Cinemachine` namespace (not legacy `Cinemachine`)
- `CinemachineCamera` for virtual cameras
- `CinemachineBrain` for camera output
- `CinemachineImpulseListener` for camera shake
- `CinemachineBasicMultiChannelPerlin` for noise

**See [`CINEMACHINE3_README.md`](CINEMACHINE3_README.md) for detailed documentation.**

---

## Gameplay Mechanics

### Solar State Swapping

The core mechanic of the game is the ability to swap solar states between the player and objects in the world.

**How It Works:**
1. Player has a SolarState component with a current state (Sun/Moon/Eclipse)
2. Objects in the world can have their own SolarState
3. SolarStateSwapInteractable objects allow players to swap states
4. When player clicks an interactable with different state, states swap
5. SolarStateMaterial updates visual appearance based on state

**Strategic Use:**
- Swap to Sun state to activate Sun-specific triggers
- Swap to Moon state to access Moon-only areas
- Swap to Eclipse state for Eclipse-exclusive mechanics
- Some puzzles require specific state combinations

### Movement

**Controls:**
- **WASD / Left Stick**: Move
- **Space**: Jump (hold for higher jump)
- **Shift**: Sprint
- **Ctrl**: Crouch (toggle)

**Movement Features:**
- Camera-relative movement
- Smooth acceleration and deceleration
- Slope handling (max 45°)
- Coyote time (jump shortly after leaving ground)
- Jump buffering (input remembered before landing)
- Variable jump height

### Health and Death

**Damage Sources:**
- KillZones (instant death)
- Custom damage systems (via HealthComponent.TakeDamage)

**Respawn Flow:**
1. Entity dies (health reaches 0)
2. Death event fires
3. RespawnableComponent detects death
4. Entity disabled (optional)
5. Wait for respawn delay
6. Entity enabled at respawn location
7. Health restored (optional)
8. Velocity reset (optional)

**Respawn Locations:**
- Original position (default)
- Last activated checkpoint
- Custom spawn point

### Interaction

**How to Interact:**
1. Point camera at interactable object
2. Object highlights when hovered
3. Click to interact
4. Interaction executes based on object type

**Interactable Types:**
- SolarStateSwapInteractable: Swap solar states
- Custom IInteractable implementations

---

## Setup Guide

### Player Setup

1. Create player GameObject
2. Add Rigidbody component:
   - Use Gravity: true
   - Is Kinematic: false
   - Interpolation: Interpolate
   - Collision Detection: Continuous Dynamic
   - Constraints: Freeze Rotation
3. Add CapsuleCollider component
4. Add RigidbodyPlayerController component
5. Assign InputActionReferences in inspector
6. Assign camera transform
7. Add PlayerInput component
8. Add PlayerInputSingleton component

### Solar State Setup

1. Add SolarState component to GameObject
2. Set initial state in inspector
3. Add SolarStateMaterial for visual changes:
   - Assign materials for each state
   - Assign target renderers
4. Add SolarStateSwapInteractable for interaction:
   - Assign player tag
   - Configure visual feedback
   - Add effects (particles, sound)

### Trigger Setup

1. Create GameObject for trigger
2. Add GenericTrigger component
3. Configure overlap box size and position
4. Add trigger options (e.g., SolarStateTriggerOption)
5. Add trigger reactions (e.g., TriggerReactionGameObjectToggle)
6. Configure layer mask for detection

### Health Setup

1. Add HealthComponent to entity
2. Configure max health and death behavior
3. Add RespawnableComponent for respawning:
   - Choose respawn location
   - Set delay and options
4. Add Checkpoint GameObjects:
   - Configure visual feedback
   - Set activator tag

### Teleporter Setup

1. Create entry GameObject with Teleporter component
2. Configure entry box size and position
3. Create destination GameObject
4. Assign destination transform to teleporter
5. Configure preservation options
6. Set layer mask for teleportable objects

---

## Related Documentation

- [`INPUTSYSTEM_README.md`](INPUTSYSTEM_README.md) - Detailed Input System integration guide
- [`CINEMACHINE3_README.md`](CINEMACHINE3_README.md) - Cinemachine 3 usage guide
- [`KILO_AGENT_README.md`](KILO_AGENT_README.md) - Coding guidelines and best practices

---

## Technical Notes

### Unity Version
- Built for Unity 6000.0+
- Uses new Object API (FindObjectsByType, FindFirstObjectByType)
- HDRP for rendering

### Input System
- Uses Unity Input System package
- Polling-based input handling
- Action map switching support

### Namespaces
- `ChildOfEclipse`: Main game namespace
- `ChildOfEclipse.Health`: Health system
- `World`: World systems (SolarState, Triggers, Teleporter)

### Coding Standards
- Follow patterns in [`KILO_AGENT_README.md`](KILO_AGENT_README.md)
- Use explicit null checks for Unity objects (no `?.` operator)
- Cache component references in Awake/Start
- Use XML documentation for public APIs

---

## Quick Reference

### Common Patterns

**Getting PlayerInput:**
```csharp
var playerInput = PlayerInputSingleton.Instance?.PlayerInput;
```

**Getting InputAction:**
```csharp
InputAction action = playerInput.actions.FindAction(actionReference.action.id);
```

**Reading Input:**
```csharp
Vector2 input = action.ReadValue<Vector2>();
if (action.WasPressedThisFrame()) { }
if (action.IsPressed()) { }
```

**Finding Player:**
```csharp
GameObject player = GameObject.FindGameObjectWithTag("Player");
```

**Changing Solar State:**
```csharp
solarState.CurrentState = SolarStateValue.Moon;
// or
solarState.SetMoonState();
```

**Applying Damage:**
```csharp
healthComponent.TakeDamage(10f);
```

**Creating Trigger:**
```csharp
var trigger = gameObject.AddComponent<GenericTrigger>();
var option = gameObject.AddComponent<SolarStateTriggerOption>();
var reaction = gameObject.AddComponent<TriggerReactionGameObjectToggle>();
```

---

## License

This project is proprietary. All rights reserved.
