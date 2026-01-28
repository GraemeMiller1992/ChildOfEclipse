Shader "Custom/WaterShader"
{
    Properties
    {
        [Header(Water Color)]
        _ShallowColor("Shallow Color", Color) = (0.2, 0.7, 0.8, 1)
        _DeepColor("Deep Color", Color) = (0.05, 0.2, 0.4, 1)
        
        [Header(Wave Settings)]
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveHeight("Wave Height", Range(0, 1)) = 0.1
        _WaveFrequency("Wave Frequency", Range(0.1, 10)) = 2.0
        
        [Header(Normal Map)]
        [MainTexture] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 2)) = 1.0
        _NormalSpeed("Normal Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
        
        [Header(Fresnel)]
        _FresnelColor("Fresnel Color", Color) = (0.8, 0.9, 1.0, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 2)) = 0.5
        
        [Header(Foam)]
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDistance("Foam Distance", Range(0, 5)) = 1.0
        _FoamSmoothness("Foam Smoothness", Range(0.01, 0.5)) = 0.1
        _FoamNoise("Foam Noise", 2D) = "white" {}
        _FoamNoiseScale("Foam Noise Scale", Float) = 1.0
        
        [Header(Depth)]
        _MaxDepth("Max Depth", Range(0.1, 20)) = 5.0
        _DepthSmoothness("Depth Smoothness", Range(0.01, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            TEXTURE2D(_FoamNoise);
            SAMPLER(sampler_FoamNoise);
            
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _WaveSpeed;
                float _WaveHeight;
                float _WaveFrequency;
                float4 _NormalMap_ST;
                float _NormalScale;
                float2 _NormalSpeed;
                half4 _FresnelColor;
                float _FresnelPower;
                float _FresnelIntensity;
                half4 _FoamColor;
                float _FoamDistance;
                float _FoamSmoothness;
                float4 _FoamNoise_ST;
                float _FoamNoiseScale;
                float _MaxDepth;
                float _DepthSmoothness;
            CBUFFER_END

            // Simple wave function
            float3 GetWaveOffset(float3 position, float time)
            {
                float wave1 = sin(position.x * _WaveFrequency + time * _WaveSpeed) * _WaveHeight;
                float wave2 = sin(position.z * _WaveFrequency * 0.7 + time * _WaveSpeed * 1.3) * _WaveHeight * 0.5;
                float wave3 = sin((position.x + position.z) * _WaveFrequency * 0.5 + time * _WaveSpeed * 0.8) * _WaveHeight * 0.3;
                
                return float3(0, wave1 + wave2 + wave3, 0);
            }

            // Calculate normal from wave displacement
            float3 GetWaveNormal(float3 position, float time)
            {
                float epsilon = 0.01;
                float3 offset = GetWaveOffset(position, time);
                float3 offsetX = GetWaveOffset(position + float3(epsilon, 0, 0), time);
                float3 offsetZ = GetWaveOffset(position + float3(0, 0, epsilon), time);
                
                float3 tangent = normalize(float3(epsilon, offsetX.y - offset.y, 0));
                float3 bitangent = normalize(float3(0, offsetZ.y - offset.y, epsilon));
                
                return normalize(cross(bitangent, tangent));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float time = _Time.y;
                
                // Apply wave displacement
                float3 waveOffset = GetWaveOffset(worldPos, time);
                worldPos += waveOffset;
                
                OUT.positionWS = worldPos;
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.uv = IN.uv;
                
                // Calculate wave normal
                float3 waveNormal = GetWaveNormal(worldPos, time);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalWS = normalize(normalWS + waveNormal * 0.5);
                
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.viewDirWS = GetWorldSpaceViewDir(worldPos);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize view direction
                float3 viewDirWS = normalize(IN.viewDirWS);
                float3 normalWS = normalize(IN.normalWS);
                
                // Sample and blend normal maps
                float2 normalUV = IN.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float2 normalOffset1 = _Time.y * _NormalSpeed;
                float2 normalOffset2 = _Time.y * _NormalSpeed * 1.5;
                
                float3 normalTS1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV + normalOffset1));
                float3 normalTS2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV * 1.5 + normalOffset2));
                float3 normalTS = normalize(normalTS1 + normalTS2);
                
                // Transform normal to world space
                float3x3 tangentToWorld = float3x3(
                    float3(1, 0, 0),
                    float3(0, 1, 0),
                    normalWS
                );
                float3 finalNormal = normalize(mul(normalTS * _NormalScale, tangentToWorld));
                
                // Calculate fresnel
                float fresnel = pow(1.0 - saturate(dot(viewDirWS, finalNormal)), _FresnelPower);
                half3 fresnelColor = _FresnelColor.rgb * fresnel * _FresnelIntensity;
                
                // Calculate depth
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float depth = LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r, _ZBufferParams);
                float surfaceDepth = IN.screenPos.w;
                float waterDepth = depth - surfaceDepth;
                
                // Depth-based color blending
                float depthFactor = saturate(waterDepth / _MaxDepth);
                depthFactor = smoothstep(0, _DepthSmoothness, depthFactor);
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);
                
                // Add fresnel to water color
                waterColor += fresnelColor;
                
                // Calculate foam
                float foamNoise = SAMPLE_TEXTURE2D(_FoamNoise, sampler_FoamNoise, IN.uv * _FoamNoise_ST.xy + _Time.y * 0.1).r;
                float foamEdge = 1.0 - smoothstep(_FoamDistance - _FoamSmoothness, _FoamDistance, waterDepth);
                float foam = foamEdge * foamNoise;
                
                // Blend foam
                half3 finalColor = lerp(waterColor, _FoamColor.rgb, foam);
                
                // Calculate alpha based on depth and fresnel
                float alpha = saturate(0.3 + depthFactor * 0.5 + fresnel * 0.2);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
