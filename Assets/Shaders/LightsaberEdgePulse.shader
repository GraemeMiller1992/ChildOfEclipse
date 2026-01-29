Shader "Custom/LightsaberEdgePulse"
{
    Properties
    {
        [Header(Edge Glow Settings)]
        _EdgeColor("Edge Color", Color) = (0, 1, 1, 1)
        _CoreColor("Core Color", Color) = (1, 1, 1, 1)
        _EdgeIntensity("Edge Intensity", Float) = 5.0
        _FresnelPower("Fresnel Power", Float) = 2.0
        _CoreSharpness("Core Sharpness", Float) = 0.1
        
        [Header(Edge Detection)]
        _EdgeThreshold("Edge Threshold", Range(0.01, 1.0)) = 0.3
        _EdgeThickness("Edge Thickness", Range(0.5, 5.0)) = 1.0
        _NormalEdgeWeight("Normal Edge Weight", Range(0.0, 1.0)) = 0.7
        _DepthEdgeWeight("Depth Edge Weight", Range(0.0, 1.0)) = 0.3
        
        [Header(Pulse Animation)]
        _PulseSpeed("Pulse Speed", Float) = 3.0
        _PulseMin("Pulse Min Intensity", Float) = 0.5
        _PulseMax("Pulse Max Intensity", Float) = 1.5
        _PulseSharpness("Pulse Sharpness", Float) = 2.0
        
        [Header(Noise Settings)]
        _NoiseSpeed("Noise Speed", Float) = 0.5
        _NoiseScale("Noise Scale", Float) = 10.0
        _NoiseAmount("Noise Amount", Float) = 0.1
        
        [Header(Motion)]
        _FlickerSpeed("Flicker Speed", Float) = 10.0
        _FlickerAmount("Flicker Amount", Float) = 0.1
        
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Transparent"
        }

        // Forward Unlit Pass
        Pass
        {
            Name "Forward Unlit"
            Tags { "LightMode" = "ForwardOnly" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 positionNDC : TEXCOORD3;
            };

            float4 _EdgeColor;
            float4 _CoreColor;
            float _EdgeIntensity;
            float _FresnelPower;
            float _CoreSharpness;
            
            float _EdgeThreshold;
            float _EdgeThickness;
            float _NormalEdgeWeight;
            float _DepthEdgeWeight;
            
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;
            float _PulseSharpness;
            
            float _NoiseSpeed;
            float _NoiseScale;
            float _NoiseAmount;
            
            float _FlickerSpeed;
            float _FlickerAmount;

            // Simplex noise function
            float3 hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453);
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float n = lerp(
                    lerp(
                        lerp(dot(hash33(i + float3(0,0,0)), f - float3(0,0,0)),
                             dot(hash33(i + float3(1,0,0)), f - float3(1,0,0)),
                             f.x),
                        lerp(dot(hash33(i + float3(0,1,0)), f - float3(0,1,0)),
                             dot(hash33(i + float3(1,1,0)), f - float3(1,1,0)),
                             f.x),
                        f.y),
                    lerp(
                        lerp(dot(hash33(i + float3(0,0,1)), f - float3(0,0,1)),
                             dot(hash33(i + float3(1,0,1)), f - float3(1,0,1)),
                             f.x),
                        lerp(dot(hash33(i + float3(0,1,1)), f - float3(0,1,1)),
                             dot(hash33(i + float3(1,1,1)), f - float3(1,1,1)),
                             f.x),
                        f.y),
                    f.z
                );
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                
                // Calculate NDC position for screen-space edge detection
                output.positionNDC = output.positionCS * (1.0 / output.positionCS.w);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // View direction and normal
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                
                // Fresnel effect (silhouette edge detection for curved surfaces)
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower);
                
                // Screen-space edge detection using normal derivatives
                // This detects sharp edges where normals change rapidly (like on cubes)
                float3 normalDerivX = ddx(normalWS);
                float3 normalDerivY = ddy(normalWS);
                float normalEdge = length(normalDerivX) + length(normalDerivY);
                normalEdge = saturate(normalEdge * _EdgeThickness);
                normalEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + 0.2, normalEdge);
                
                // Screen-space edge detection using depth derivatives
                // This detects depth discontinuities at edges
                float depthDerivX = ddx(input.positionNDC.z);
                float depthDerivY = ddy(input.positionNDC.z);
                float depthEdge = abs(depthDerivX) + abs(depthDerivY);
                depthEdge = saturate(depthEdge * _EdgeThickness * 100.0);
                depthEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + 0.2, depthEdge);
                
                // Combine all edge detection methods
                float geometricEdge = lerp(normalEdge, depthEdge, _DepthEdgeWeight);
                geometricEdge = lerp(fresnel, geometricEdge, _NormalEdgeWeight);
                
                // Animated pulse (sine wave with sharpness)
                float time = _Time.y * _PulseSpeed;
                float pulse = sin(time) * 0.5 + 0.5; // 0 to 1
                pulse = pow(pulse, _PulseSharpness); // Sharpen the pulse
                float pulseIntensity = lerp(_PulseMin, _PulseMax, pulse);
                
                // Add high-frequency flicker for energy feel
                float flicker = frac(sin(dot(input.positionWS.xy, float2(12.9898, 78.233)) + _Time.y * _FlickerSpeed) * 43758.5453);
                flicker = lerp(1.0 - _FlickerAmount, 1.0 + _FlickerAmount, flicker);
                
                // Add noise movement along edges
                float3 noisePos = input.positionWS * _NoiseScale + _Time.y * _NoiseSpeed;
                float noiseVal = noise3D(noisePos) * _NoiseAmount;
                
                // Combine effects
                float edgeMask = saturate(geometricEdge * pulseIntensity * flicker + noiseVal);
                
                // Create core-to-edge gradient
                float coreMask = saturate(geometricEdge / _CoreSharpness);
                float4 finalColor = lerp(_CoreColor, _EdgeColor, coreMask);
                
                // Apply intensity and HDR bloom
                float intensity = _EdgeIntensity * pulseIntensity * flicker;
                float3 emission = finalColor.rgb * intensity * edgeMask;
                
                // Add extra brightness to core
                emission += _CoreColor.rgb * (1.0 - coreMask) * intensity * 0.5;
                
                return float4(emission, 1.0);
            }
            ENDHLSL
        }
    }
}