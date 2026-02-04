# Health System Documentation

This directory contains all health-related scripts for the Child of Eclipse project.

## Components

### HealthComponent.cs
The core health component that manages health, damage, healing, and death mechanics for any game object.

**Key Features:**
- Configurable max health and current health
- Damage and healing methods with events
- Death handling with destroy/disable options
- Health percentage calculation
- Revive and reset functionality

**Public Methods:**
- `TakeDamage(float damageAmount)` - Apply damage to the entity
- `Heal(float healAmount)` - Heal the entity
- `SetHealth(float healthValue)` - Set health to a specific value
- `SetMaxHealth(float newMaxHealth, bool scaleCurrentHealth)` - Set maximum health
- `Kill()` - Instantly kill the entity
- `Revive()` - Revive the entity
- `ResetHealth()` - Reset health to maximum

**Events:**
- `OnDamageTaken` - Invoked when damage is taken
- `OnHealed` - Invoked when healed
- `OnDeath` - Invoked when health reaches zero
- `OnHealthChanged` - Invoked when health changes

---

### KillZone.cs
A trigger-based kill zone that instantly kills any game object with a HealthComponent that enters its collider.

**Key Features:**
- Trigger-based detection
- Tag and layer filtering
- One-time kill option
- Kill delay support
- Gizmo visualization

**Inspector Settings:**
- `Target Tags` - Filter by specific tags (empty = all)
- `Target Layers` - Filter by specific layers
- `One Time Kill` - Only kill each object once
- `Kill Delay` - Delay before killing (0 = instant)

**Events:**
- `OnObjectEntered` - Invoked when an object enters the zone
- `OnObjectKilled` - Invoked when an object is killed

**Public Methods:**
- `KillTarget(GameObject target)` - Manually kill a specific object
- `ResetKillZone()` - Clear tracked objects for one-time kill
- `RemoveFromKilledList(GameObject target)` - Allow object to be killed again

---

### SphereCastDamageZone.cs
A sphere cast-based damage zone that damages objects with HealthComponents along a directional path like a laser beam.

**Key Features:**
- Sphere cast detection along a directional path
- Configurable damage amount and rate
- Adjustable sphere radius and max distance
- Tag and layer filtering
- Per-target damage tracking
- Gizmo visualization

**Inspector Settings:**
- `Damage Amount` - Amount of damage to apply per hit
- `Damage Interval` - How often to apply damage (0 = once per frame)
- `Sphere Radius` - Radius of the sphere cast (width of the laser)
- `Max Distance` - Maximum distance of the sphere cast
- `Cast Direction` - Direction of the sphere cast in local space (default: forward)
- `Target Tags` - Filter by specific tags (empty = all)
- `Target Layers` - Filter by specific layers
- `Damage Once Per Interval` - Only damage each target once per interval

**Events:**
- `OnObjectDamaged` - Invoked when an object is damaged (passes object and damage amount)
- `OnHitDetected` - Invoked when the damage zone hits something (passes the object)
- `OnObjectsInZone` - Invoked each update with all currently hit objects

**Public Methods:**
- `TriggerDamage()` - Manually trigger a sphere cast and apply damage
- `ResetDamageTimers()` - Reset the damage tracking timers
- `SetDamageAmount(float amount)` - Set the damage amount
- `SetSphereRadius(float radius)` - Set the sphere radius
- `SetMaxDistance(float distance)` - Set the maximum distance
- `SetCastDirection(Vector3 direction)` - Set the cast direction in local space
- `RemoveTargetFromTracking(GameObject target)` - Remove a target from damage tracking
- `ClearAllTracking()` - Clear all tracked targets

**Public Properties:**
- `HitObjects` - Gets the current list of objects being hit
- `IsHittingObjects` - Gets whether the damage zone is currently hitting any objects

**Usage Example:**
```csharp
// Create a laser beam that damages enemies
SphereCastDamageZone laser = gameObject.AddComponent<SphereCastDamageZone>();
laser.SetDamageAmount(25f);
laser.SetSphereRadius(0.3f);
laser.SetMaxDistance(50f);
laser.damageInterval = 0.5f; // Damage every 0.5 seconds
```

---

### Checkpoint.cs
Checkpoint system for respawning players.

**Key Features:**
- Save player position and rotation
- Trigger-based activation
- Optional visual feedback

---

### RespawnableComponent.cs
Component that enables objects to be respawned at checkpoints.

**Key Features:**
- Save initial state
- Respawn at checkpoint location
- Health restoration on respawn

## Usage Patterns

### Creating a Damage Zone

**Kill Zone (Trigger-based):**
1. Create a GameObject with a Collider (Box, Sphere, or Capsule)
2. Add the `KillZone` component
3. Set the collider to Trigger
4. Configure target tags/layers as needed

**Sphere Cast Damage Zone (Laser):**
1. Create a GameObject
2. Add the `SphereCastDamageZone` component
3. Configure the damage amount, radius, and distance
4. Rotate the GameObject to aim the laser
5. Configure target tags/layers as needed

### Connecting to HealthComponent

All damage zones automatically look for `HealthComponent` on detected objects. Simply add a `HealthComponent` to any GameObject that should be damageable.

### Event Integration

Both damage zones expose UnityEvents that can be connected in the Inspector to:
- Play sound effects
- Spawn particles
- Update UI
- Trigger other game logic
