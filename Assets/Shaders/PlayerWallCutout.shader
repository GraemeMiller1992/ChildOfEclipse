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
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_FILTERING
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Material properties
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            
            CBUFFER_START(UnityPerMaterial)
            float _CutoutRadius;
            float _CutoutSoftness;
            float _WallTransparency;
            float4 _EdgeColor;
            float _EdgeIntensity;
            float _EdgeWidth;
            float4 _BaseColor;
            float _Metallic;
            float _Smoothness;
            float _BumpScale;
            CBUFFER_END
            
            // Global player properties (set by PlayerWallCutoutController)
            uniform float3 _PlayerPosition;
            uniform float4 _PlayerScreenPos; // (x, y, depth, 1)
            uniform float _PlayerRadius;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // --- Cutout Mask Calculation ---
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
                
                // Edge Glow Mask
                float edgeMask = smoothstep(screenRadius - _EdgeWidth * 0.1, screenRadius, distToPlayerSS) * 
                                 smoothstep(screenRadius + _EdgeWidth * 0.1, screenRadius, distToPlayerSS);
                
                edgeMask *= (isOccluding ? 1.0 : 0.0);
                
                // --- PBR Lighting ---
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = 0; // Initialize properly if needed
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = 1.0;

                // Normal Mapping
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 bitangentWS = cross(inputData.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, inputData.normalWS));
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1); // Already applied to inputData.normalWS
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = albedo.a;
                surfaceData.emission = 0;
                
                // Alpha handling
                if (_PlayerRadius > 0.5)
                {
                    surfaceData.alpha = lerp(surfaceData.alpha, surfaceData.alpha * _WallTransparency, cutoutMask);
                }

                // Final Lighting calculation
                float4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                
                // Apply Edge Glow on top of lighting
                finalColor.rgb = lerp(finalColor.rgb, _EdgeColor.rgb, edgeMask * _EdgeIntensity);
                
                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
