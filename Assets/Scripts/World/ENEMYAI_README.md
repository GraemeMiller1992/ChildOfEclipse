# Enemy AI System

A modular AI system for Unity enemies that uses NavMesh agents. The system consists of four main components that work together to create intelligent enemy behavior.

## Components

### 1. NavMeshAgentPatrol
Controls a NavMeshAgent to patrol between multiple waypoints.

**Features:**
- Three patrol modes: Loop, PingPong, Random
- Configurable wait times at waypoints
- Dynamic waypoint management (add/remove at runtime)
- Debug visualization in Scene view

**Key Settings:**
- **Waypoints**: Array of transforms defining the patrol path
- **Patrol Mode**: How the agent moves between waypoints
- **Wait At Waypoints**: Whether to pause at each waypoint
- **Min/Max Wait Time**: Random wait duration range

**Usage:**
```csharp
// Start patrolling
patrolComponent.StartPatrol();

// Stop patrolling
patrolComponent.StopPatrol();

// Add a waypoint at runtime
patrolComponent.AddWaypoint(newWaypointTransform);
```

---

### 2. NavMeshAgentChase
Controls a NavMeshAgent to chase a target when detected.

**Features:**
- Configurable detection range and field of view
- Line of sight checking with raycasting
- Speed multipliers when chasing
- Automatic target finding by tag
- Debug visualization

**Key Settings:**
- **Target**: The transform to chase (optional - can use tag)
- **Target Tag**: Tag to automatically find the target
- **Detection Range**: Maximum distance to detect target
- **Field Of View**: Detection angle (360 = omnidirectional)
- **Obstacle Layers**: Layers that block line of sight
- **Require Line Of Sight**: Whether to check for obstacles
- **Chase Speed Multiplier**: Speed boost when chasing
- **Stop Distance**: Distance to stop from target

**Events:**
- `OnTargetDetected`: Fired when target is first detected
- `OnTargetLost`: Fired when target is lost after delay

**Usage:**
```csharp
// Set target manually
chaseComponent.SetTarget(playerTransform);

// Find target by tag
chaseComponent.SetTargetByTag("Player");

// Check if target is in attack range
if (chaseComponent.IsTargetInAttackRange(attackRange))
{
    // Attack!
}
```

---

### 3. NavMeshAgentAttack
Handles attack behavior when a target is within range.

**Features:**
- Configurable attack range and damage
- Attack cooldown system
- Automatic or manual trigger modes
- Optional attack visual GameObject
- Integration with HealthComponent

**Key Settings:**
- **Target**: The transform to attack
- **Trigger Mode**: Manual (controlled by EnemyAI) or Automatic
- **Attack Range**: Maximum distance to attack
- **Damage**: Damage dealt per attack
- **Attack Cooldown**: Time between attacks
- **Look At Target**: Whether to face target during attack
- **Attack Visual**: GameObject to enable during attack

**Events:**
- `OnAttackStarted`: Fired when attack begins
- `OnAttackHit`: Fired when attack hits target
- `OnAttackEnded`: Fired when attack completes

**Usage:**
```csharp
// Try to attack (returns true if successful)
if (attackComponent.TryAttack())
{
    // Attack initiated
}

// Check if can attack
if (attackComponent.CanAttack && attackComponent.IsTargetInRange())
{
    attackComponent.PerformAttack();
}
```

---

### 4. EnemyAI
State machine controller that coordinates Patrol, Chase, and Attack behaviors.

**Features:**
- Automatic state transitions based on conditions
- Priority-based state management
- Configurable return-to-patrol behavior
- State change events
- Debug logging and visualization

**States:**
- **Patrol**: Enemy patrols between waypoints
- **Chase**: Enemy chases detected target
- **Attack**: Enemy attacks target in range
- **Idle**: Enemy waits (transitional state)

**Key Settings:**
- **Patrol/Chase/Attack Components**: References to AI behaviors
- **Initial State**: Starting AI state
- **Enable On Start**: Whether to enable AI automatically
- **Return To Patrol On Lose Target**: Resume patrol after losing target
- **Return To Patrol Delay**: Time to wait before resuming patrol
- **Log State Changes**: Debug logging

**Events:**
- `OnStateChanged`: Fired when AI state changes (oldState, newState)
- `OnAIEnabled`: Fired when AI is enabled
- `OnAIDisabled`: Fired when AI is disabled

**Usage:**
```csharp
// Enable AI
enemyAI.EnableAI();

// Disable AI
enemyAI.DisableAI();

// Force a specific state
enemyAI.ForceState(EnemyAI.AIState.Patrol);

// Set target for chase and attack
enemyAI.SetTarget(playerTransform);
```

---

## Setup Guide

### Basic Setup

1. **Create Enemy GameObject**
   - Add a NavMeshAgent component
   - Bake a NavMesh for your scene

2. **Add AI Components**
   - Add `NavMeshAgentPatrol` component
   - Add `NavMeshAgentChase` component
   - Add `NavMeshAgentAttack` component
   - Add `EnemyAI` component (will auto-find other components)

3. **Configure Patrol**
   - Create empty GameObjects as waypoints
   - Assign waypoints to the Patrol component
   - Set patrol mode and wait times

4. **Configure Chase**
   - Set target tag (e.g., "Player")
   - Adjust detection range and field of view
   - Set obstacle layers for line of sight

5. **Configure Attack**
   - Set attack range and damage
   - Set attack cooldown
   - Optionally add attack visual GameObject

6. **Configure EnemyAI**
   - Set initial state (usually Patrol)
   - Enable on start
   - Configure return-to-patrol behavior

### Advanced Setup

**Multiple Enemy Types:**
- Different patrol routes for different enemies
- Varying detection ranges and speeds
- Different attack patterns and damage

**Integration with SolarState:**
- Use `SolarStateNavAgentStopper` to pause AI during certain solar states
- Combine with other solar state behaviors

**Custom Events:**
```csharp
// Subscribe to state changes
enemyAI.OnStateChanged += (oldState, newState) => {
    Debug.Log($"State changed from {oldState} to {newState}");
    // Update animations, sounds, etc.
};

// Subscribe to attack events
attackComponent.OnAttackStarted += () => {
    // Play attack animation
};
```

---

## State Transition Flow

```
Idle
  ↓ (patrol available)
Patrol
  ↓ (target detected)
Chase
  ↓ (target in attack range)
Attack
  ↓ (target out of range)
Chase
  ↓ (target lost)
Idle
  ↓ (after delay)
Patrol
```

---

## Tips and Best Practices

1. **Waypoint Placement**
   - Place waypoints on the NavMesh surface
   - Ensure waypoints are within NavMesh bounds
   - Use sufficient spacing between waypoints

2. **Detection Tuning**
   - Start with generous detection ranges
   - Adjust field of view based on enemy type
   - Test line of sight with various obstacle configurations

3. **Performance**
   - Disable debug gizmos in production builds
   - Use appropriate detection ranges (not too large)
   - Consider object pooling for many enemies

4. **Balancing**
   - Test patrol, chase, and attack together
   - Adjust speeds and cooldowns for difficulty
   - Consider player movement speed when tuning chase

5. **Debugging**
   - Enable state change logging during development
   - Use Scene view gizmos to visualize detection ranges
   - Monitor AI state in real-time

---

## Troubleshooting

**Enemy not moving:**
- Check NavMeshAgent is enabled
- Verify NavMesh is baked correctly
- Ensure waypoints are on NavMesh

**Enemy not detecting player:**
- Check player has correct tag
- Verify detection range is sufficient
- Check field of view angle
- Ensure line of sight isn't blocked

**Enemy not attacking:**
- Verify attack range is sufficient
- Check attack cooldown isn't too long
- Ensure target has HealthComponent
- Verify attack trigger mode

**Enemy not returning to patrol:**
- Check "Return To Patrol On Lose Target" is enabled
- Verify patrol component has waypoints
- Adjust return delay if needed

---

## Example Scene Setup

1. Create a plane with NavMeshSurface component
2. Bake the NavMesh
3. Create player GameObject with "Player" tag
4. Create enemy GameObject with NavMeshAgent
5. Add all AI components
6. Create 3-4 waypoint GameObjects
7. Assign waypoints to Patrol component
8. Play and test!

---

## Component Dependencies

```
EnemyAI (Required)
├── NavMeshAgentPatrol (Optional)
├── NavMeshAgentChase (Optional)
│   └── NavMeshAgent (Required by all)
└── NavMeshAgentAttack (Optional)
    └── Health.HealthComponent (On target, for damage)
```

---

## Extending the System

The system is designed to be modular and extensible:

**Add New States:**
- Extend the `AIState` enum in `EnemyAI`
- Add state handling in `ChangeState()` and `UpdateState()`
- Create new component if needed

**Custom Behaviors:**
- Create new components following the same pattern
- Integrate with EnemyAI via events or direct calls
- Use component references for coordination

**Integration Points:**
- Subscribe to component events for custom logic
- Override state transitions via `ForceState()`
- Extend components with inheritance for specialized enemies
