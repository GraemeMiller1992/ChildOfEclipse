Shader "Custom/PlayerWallCutout"
{
    Properties
    {
        [Header(Cutout Settings)]
        _CutoutRadius("Cutout Radius", Range(0.1, 5.0)) = 1.5
        _CutoutSoftness("Cutout Softness", Range(0.0, 1.0)) = 0.3
        _WallTransparency("Wall Transparency", Range(0.0, 1.0)) = 0.3
        
        [Header(Edge Glow Settings)]
        _EdgeColor("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeIntensity("Edge Intensity", Range(0.0, 2.0)) = 0.5
        _EdgeWidth("Edge Width", Range(0.01, 0.5)) = 0.1
        
        [Header(Base Material)]
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        // Main wall cutout pass
        Pass
        {
            Name "WallCutout"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Material properties
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            float _CutoutRadius;
            float _CutoutSoftness;
            float _WallTransparency;
            float4 _EdgeColor;
            float _EdgeIntensity;
            float _EdgeWidth;
            float4 _BaseColor;
            float _Metallic;
            float _Smoothness;
            
            // Global player properties (set by PlayerWallCutoutController)
            uniform float3 _PlayerPosition;
            uniform float4 _PlayerScreenPos; // (x, y, depth, 1)
            uniform float _PlayerRadius;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Sample base texture
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Screen space coordinates (0-1)
                float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                
                // Corrected screen space distance (perfect circle)
                float2 diffSS = screenUV - _PlayerScreenPos.xy;
                diffSS.x *= aspect; 
                float distToPlayerSS = length(diffSS);
                
                // Occlusion check (only cutout if wall is in front of player)
                float fragmentDepth = input.positionCS.w;
                float playerDepth = _PlayerScreenPos.z;
                bool isOccluding = fragmentDepth < playerDepth - 0.1;
                
                // Calculate cutout mask
                float screenRadius = 0.05 * _CutoutRadius; 
                float cutoutMask = 1.0 - smoothstep(screenRadius - _CutoutSoftness * 0.1, screenRadius + _CutoutSoftness * 0.1, distToPlayerSS);
                
                // Only apply if occluding
                cutoutMask *= (isOccluding ? 1.0 : 0.0);
                
                // Edge Glow
                float edgeMask = smoothstep(screenRadius - _EdgeWidth * 0.1, screenRadius, distToPlayerSS) * 
                                 smoothstep(screenRadius + _EdgeWidth * 0.1, screenRadius, distToPlayerSS);
                
                edgeMask *= (isOccluding ? 1.0 : 0.0);
                
                // Apply final values
                float3 finalColor = baseColor.rgb;
                finalColor = lerp(finalColor, _EdgeColor.rgb, edgeMask * _EdgeIntensity);
                
                // Alpha handling
                float alpha = 1.0;
                if (_PlayerRadius > 0.5)
                {
                    alpha = lerp(1.0, _WallTransparency, cutoutMask);
                }
                
                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
