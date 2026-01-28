# Cinemachine 3 Integration Guide

This document explains the proper usage of Cinemachine 3.x components in this project and highlights the differences from legacy Cinemachine 2.x.

## Overview

This project uses **Cinemachine 3.x** (Unity.Cinemachine namespace), which is a complete rewrite of Cinemachine with significant API changes. **Do not use legacy Cinemachine 2.x components** (Cinemachine namespace).

## Key Differences: Cinemachine 3 vs Legacy Cinemachine 2

| Feature | Cinemachine 3.x (Use This) | Legacy Cinemachine 2.x (Do NOT Use) |
|---------|---------------------------|--------------------------------------|
| Namespace | `Unity.Cinemachine` | `Cinemachine` |
| Virtual Camera | `CinemachineCamera` | `CinemachineVirtualCamera` |
| Brain | `CinemachineBrain` | `CinemachineBrain` |
| Impulse Listener | `CinemachineImpulseListener` | `CinemachineImpulseListener` |
| Impulse Definition | `CinemachineImpulseDefinition` | `CinemachineImpulseDefinition` |
| Noise | `CinemachineBasicMultiChannelPerlin` | `CinemachineBasicMultiChannelPerlin` |

## Cinemachine 3 Components

### 1. CinemachineCamera

The main virtual camera component that defines camera behavior.

```csharp
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    [Tooltip("Cinemachine Virtual Camera for the player.")]
    public CinemachineCamera virtualCamera;
    
    private void Awake()
    {
        // Find CinemachineCamera in scene if not assigned
        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }
    }
    
    private void OnEnable()
    {
        if (virtualCamera != null)
        {
            // Set follow target
            virtualCamera.Follow = playerTransform;
            virtualCamera.LookAt = null;
            
            // Configure field of view
            virtualCamera.Lens.FieldOfView = settings.cameraFOV;
        }
    }
}
```

**Reference**: [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs:22-23, 112-145)

### 2. CinemachineImpulseListener

Handles camera shake impulses from impacts or events.

```csharp
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    private CinemachineImpulseListener impulseListener;
    
    private void SetupImpulseListener()
    {
        impulseListener = GetComponent<CinemachineImpulseListener>();
        if (impulseListener == null)
        {
            impulseListener = gameObject.AddComponent<CinemachineImpulseListener>();
        }
        
        if (impulseListener != null)
        {
            impulseListener.ReactionSettings.AmplitudeGain = settings.cameraShakeIntensity;
            impulseListener.ReactionSettings.FrequencyGain = 1f;
            impulseListener.ReactionSettings.Duration = settings.cameraShakeDuration;
        }
    }
}
```

**Reference**: [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs:52, 238-252)

### 3. CinemachineBasicMultiChannelPerlin

Adds procedural noise to camera for subtle movement (breathing, idle sway).

```csharp
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    private void OnEnable()
    {
        if (virtualCamera != null)
        {
            // Configure noise for subtle idle movement
            var noise = virtualCamera.AddComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise != null)
            {
                noise.AmplitudeGain = 0.1f;
                noise.FrequencyGain = 0.5f;
                noise.NoiseProfile = null; // Use default noise
            }
        }
    }
}
```

**Reference**: [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs:138-144)

## ❌ Legacy Components to Avoid

### Do NOT Use These Legacy Components:

```csharp
// ❌ WRONG - Legacy Cinemachine 2.x
using Cinemachine;

public class BadExample : MonoBehaviour
{
    // These are legacy components - DO NOT USE
    private CinemachineVirtualCamera virtualCamera;      // Legacy
    private CinemachineBrain brain;                       // Legacy (but similar in 3.x)
    private CinemachineImpulseDefinition impulseDef;     // Legacy
    private CinemachineTargetingComponent targeting;     // Legacy
}
```

### Use These Cinemachine 3 Components Instead:

```csharp
// ✅ CORRECT - Cinemachine 3.x
using Unity.Cinemachine;

public class GoodExample : MonoBehaviour
{
    // These are Cinemachine 3.x components
    private CinemachineCamera virtualCamera;             // Cinemachine 3
    private CinemachineBrain brain;                      // Cinemachine 3
    private CinemachineImpulseListener impulseListener;  // Cinemachine 3
    private CinemachineBasicMultiChannelPerlin noise;    // Cinemachine 3
}
```

## Common Cinemachine 3 Patterns

### Pattern 1: Camera Follow Player

```csharp
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    public CinemachineCamera virtualCamera;
    private Transform playerTransform;
    
    private void OnEnable()
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = playerTransform;
            virtualCamera.LookAt = null;
        }
    }
}
```

### Pattern 2: Camera Shake on Impact

```csharp
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    private CinemachineImpulseListener impulseListener;
    
    public void ShakeCamera(float intensity)
    {
        // Manual shake implementation (alternative to impulses)
        isShaking = true;
        shakeTimer = settings.cameraShakeDuration;
        shakeIntensity = intensity;
    }
    
    public void TriggerImpulse(Vector3 velocity, Vector3 position)
    {
        // Note: Cinemachine 3 impulse system is different from 2.x
        // Impulse generation may require different approach
        // Currently using manual shake in this project
    }
}
```

**Reference**: [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs:212-233)

### Pattern 3: Add Noise for Idle Movement

```csharp
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    public CinemachineCamera virtualCamera;
    
    private void AddIdleNoise()
    {
        var noise = virtualCamera.AddComponent<CinemachineBasicMultiChannelPerlin>();
        noise.AmplitudeGain = 0.1f;      // Movement amount
        noise.FrequencyGain = 0.5f;      // Movement speed
        noise.NoiseProfile = null;       // Use default noise profile
    }
}
```

## Setup in Unity Editor

### Adding Cinemachine 3 Components

1. **Install Cinemachine 3.x** via Package Manager
2. **Add CinemachineCamera** to scene:
   - Right-click in Hierarchy → Cinemachine → Cinemachine Camera
3. **Configure Camera Settings**:
   - Set Follow target to player transform
   - Configure Lens (Field of View, Near Clip Plane, Far Clip Plane)
   - Add noise components if needed

### Inspector Configuration

For [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs:16-23):

```csharp
[Header("References")]
[Tooltip("Reference to GameSettings ScriptableObject.")]
public GameSettings settings;

[Tooltip("Transform of the camera (child of player).")]
public Transform cameraTransform;

[Tooltip("Cinemachine Virtual Camera for the player.")]
public CinemachineCamera virtualCamera;
```

## Best Practices

### ✅ Do's

1. **Use `Unity.Cinemachine` namespace** for all Cinemachine 3 components
2. **Use `CinemachineCamera`** instead of `CinemachineVirtualCamera`
3. **Use `CinemachineImpulseListener`** for camera shake
4. **Use `CinemachineBasicMultiChannelPerlin`** for procedural noise
5. **Reference camera via `FindFirstObjectByType<CinemachineCamera>()`** if not assigned
6. **Configure camera in `OnEnable()`** to ensure proper initialization

### ❌ Don'ts

1. **Do NOT use `Cinemachine` namespace** (legacy 2.x)
2. **Do NOT use `CinemachineVirtualCamera`** (legacy component)
3. **Do NOT use legacy targeting components** like `CinemachineHardLookAt`
4. **Do NOT mix Cinemachine 2.x and 3.x components** in the same project
5. **Do NOT use legacy impulse generation methods** without checking Cinemachine 3 documentation

## Finding Components

When searching for Cinemachine components in code:

```csharp
// ✅ CORRECT - Find Cinemachine 3 components
var camera = FindFirstObjectByType<CinemachineCamera>();
var brain = FindFirstObjectByType<CinemachineBrain>();
var listener = GetComponent<CinemachineImpulseListener>();

// ❌ WRONG - These won't find Cinemachine 3 components
var oldCamera = FindObjectOfType<CinemachineVirtualCamera>();
var oldBrain = FindObjectOfType<CinemachineBrain>(); // This might work but is legacy
```

## Camera Shake Implementation

This project uses a custom camera shake implementation alongside Cinemachine:

```csharp
private void UpdateCameraShake()
{
    if (!isShaking) return;
    
    shakeTimer -= Time.deltaTime;
    
    if (shakeTimer <= 0)
    {
        isShaking = false;
        cameraTransform.localPosition = originalCameraLocalPosition;
        return;
    }
    
    float currentIntensity = shakeIntensity * (shakeTimer / settings.cameraShakeDuration);
    Vector3 shakeOffset = Random.insideUnitSphere * currentIntensity;
    cameraTransform.localPosition = originalCameraLocalPosition + shakeOffset;
}
```

**Reference**: [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs:187-206)

## Related Files

- [`PlayerCamera.cs`](Assets/Scripts/Player/PlayerCamera.cs) - Main camera controller using Cinemachine 3
- [`PlayerMovement.cs`](Assets/Scripts/Player/PlayerMovement.cs) - Player movement that references camera
- [`ObjectManipulator.cs`](Assets/Scripts/Telekinesis/ObjectManipulator.cs) - Uses camera for raycasting

## Migration Notes

If you're migrating from Cinemachine 2.x to 3.x:

1. **Update namespace**: Change `using Cinemachine;` to `using Unity.Cinemachine;`
2. **Replace components**: 
   - `CinemachineVirtualCamera` → `CinemachineCamera`
   - Keep `CinemachineBrain` (similar in both versions)
3. **Update API calls**: Many properties and methods have changed names
4. **Review impulse system**: The impulse system has significant changes in 3.x
5. **Check targeting**: Legacy targeting components may not have direct equivalents

## Troubleshooting

### Issue: Camera not following player

**Solution**: Ensure `virtualCamera.Follow` is set to the player transform in `OnEnable()`.

### Issue: Camera shake not working

**Solution**: Check that `CinemachineImpulseListener` is properly configured with `ReactionSettings`.

### Issue: Can't find CinemachineCamera

**Solution**: Use `FindFirstObjectByType<CinemachineCamera>()` instead of `FindObjectOfType<CinemachineVirtualCamera>()`.

### Issue: Legacy components causing conflicts

**Solution**: Remove all legacy Cinemachine 2.x components and replace with Cinemachine 3.x equivalents.

## Additional Resources

- [Cinemachine 3.x Documentation](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html)
- [Cinemachine 3.x API Reference](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/api/index.html)
