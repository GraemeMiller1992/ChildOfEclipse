Shader "Custom/PortalPassThroughURP"
{
    Properties
    {
        [Header(Surface)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5

        [Header(Cutout)]
        _CutoutSoftness("Cutout Softness", Range(0.01, 0.5)) = 0.1
        _NoiseDistortion("Edge Noise Distortion", Range(0, 0.5)) = 0.15
        _EdgeNoiseSpeed("Edge Noise Speed", Float) = 2.0
        _EdgeNoiseScale("Edge Noise Scale", Float) = 3.0

        [Header(Glow)]
        [HDR]_GlowColor("Glow Color", Color) = (0.5, 1.0, 2.0, 1)
        _EdgeWidth("Edge Width", Range(0.01, 0.5)) = 0.12
        _EdgeGlowIntensity("Edge Glow Intensity", Range(0, 15)) = 6.0
        _PulseSpeed("Pulse Speed", Float) = 2.0
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PortalLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend Off
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _BumpMap_ST;
                float  _BumpScale;
                float  _Metallic;
                float  _Smoothness;
                float  _CutoutSoftness;
                float  _NoiseDistortion;
                float  _EdgeNoiseSpeed;
                float  _EdgeNoiseScale;
                float4 _GlowColor;
                float  _EdgeWidth;
                float  _EdgeGlowIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);

            #define MAX_CONTACTS 8

            float3 _ContactPositions[MAX_CONTACTS];
            float4 _ContactData[MAX_CONTACTS];
            float3 _PortalNormal;
            int    _ContactCount;

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += noise(p) * a; p *= 2.02; a *= 0.5;
                v += noise(p) * a; p *= 2.03; a *= 0.5;
                v += noise(p) * a; p *= 2.01; a *= 0.5;
                v += noise(p) * a;
                return v;
            }

            float3x3 GetPortalBasis()
            {
                float3 normal = normalize(_PortalNormal);
                float3 up = abs(normal.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 tangent = normalize(cross(up, normal));
                float3 bitangent = cross(normal, tangent);
                return float3x3(tangent, bitangent, normal);
            }

            float2 ProjectOntoPortal(float3 deltaWS, float3x3 basis)
            {
                float3 projected = mul(basis, deltaWS);
                return projected.xy;
            }

            float GetShapeDist(float2 delta, float shape, float radius, float shapeParam)
            {
                if (shape < 0.5)
                {
                    return length(delta) / max(radius, 0.001);
                }
                else if (shape < 1.5)
                {
                    float halfH = shapeParam * 0.5;
                    float2 scaledDelta = delta / max(radius, 0.001);
                    float scaledHalfH = halfH / max(radius, 0.001);
                    float bodyDist = length(max(abs(scaledDelta) - float2(0, scaledHalfH - 1.0), 0.0));
                    float capDist = length(scaledDelta - float2(0, scaledHalfH - 1.0));
                    return min(bodyDist, min(capDist, length(scaledDelta + float2(0, scaledHalfH - 1.0))));
                }
                else
                {
                    float aspect = shapeParam;
                    float2 halfSize = float2(aspect, 1.0);
                    float2 scaledDelta = delta / max(radius, 0.001);
                    float2 d = abs(scaledDelta) - halfSize;
                    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
                }
            }

            void ComputeCutout(float3 fragWS, float3 contactWS, float radius, float shape, float shapeParam, float transition,
                               out float cutoutMask, out float edgeMask)
            {
                float3x3 basis = GetPortalBasis();
                float2 delta = ProjectOntoPortal(fragWS - contactWS, basis);

                float angle = atan2(delta.y, delta.x);
                float time = _Time.y * _EdgeNoiseSpeed;
                float noiseVal = fbm(float2(angle * _EdgeNoiseScale, time));
                float distortion = 1.0 + (noiseVal - 0.5) * 2.0 * _NoiseDistortion;

                float dist = GetShapeDist(delta, shape, radius, shapeParam) / distortion;
                float scaledRadius = transition;

                cutoutMask = 1.0 - smoothstep(
                    scaledRadius - _CutoutSoftness,
                    scaledRadius + _CutoutSoftness,
                    dist);

                float innerEdge = smoothstep(scaledRadius - _EdgeWidth, scaledRadius, dist);
                float outerEdge = smoothstep(scaledRadius + _EdgeWidth, scaledRadius, dist);
                edgeMask = innerEdge * outerEdge * transition;

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + dist * 10.0) * _PulseAmount;
                edgeMask *= pulse;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs norInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = norInputs.normalWS;
                output.tangentWS = norInputs.tangentWS;
                output.bitangentWS = norInputs.bitangentWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float3x3 tangentToWorld = float3x3(input.tangentWS, input.bitangentWS, normalWS);

                float4 baseMapSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float3 baseColor = baseMapSample.rgb * _BaseColor.rgb;

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uv), _BumpScale);
                normalWS = normalize(mul(normalTS, tangentToWorld));

                Light mainLight = GetMainLight();
                float3 lightColor = mainLight.color;
                float3 lightDir = mainLight.direction;
                float attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                float NdotL = saturate(abs(dot(normalWS, lightDir)));
                float3 diffuse = lightColor * NdotL * attenuation;

                float3 halfVector = normalize(lightDir + viewDirWS);
                float NdotH = saturate(abs(dot(normalWS, halfVector)));
                float shininess = exp2(10.0 * _Smoothness + 1.0);
                float3 specular = lightColor * pow(NdotH, shininess) * attenuation * lerp(0.04, 1.0, _Metallic);

                float3 ambient = SampleSH(normalWS) * baseColor;

                float3 surfaceColor = (diffuse + ambient) * baseColor * (1.0 - _Metallic) + specular * _Metallic;
                float surfaceAlpha = 1.0;

                float totalCutout = 0.0;
                float totalEdge = 0.0;

                for (int i = 0; i < MAX_CONTACTS; i++)
                {
                    if (i >= _ContactCount) break;

                    float contactTransition = _ContactData[i].x;
                    float contactRadius = _ContactData[i].y;
                    float contactShape = _ContactData[i].z;
                    float contactParam = _ContactData[i].w;
                    if (contactTransition < 0.001) continue;

                    float cutoutMask, edgeMask;
                    ComputeCutout(input.positionWS, _ContactPositions[i], contactRadius, contactShape, contactParam, contactTransition, cutoutMask, edgeMask);

                    totalCutout = max(totalCutout, cutoutMask);
                    totalEdge = max(totalEdge, edgeMask);
                }

                surfaceAlpha *= (1.0 - totalCutout);

                clip(surfaceAlpha - 0.01);

                float3 finalColor = surfaceColor + _GlowColor.rgb * totalEdge * _EdgeGlowIntensity;

                finalColor = MixFog(finalColor, -input.positionWS);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PortalGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _BumpMap_ST;
                float  _BumpScale;
                float  _Metallic;
                float  _Smoothness;
                float  _CutoutSoftness;
                float  _NoiseDistortion;
                float  _EdgeNoiseSpeed;
                float  _EdgeNoiseScale;
                float4 _GlowColor;
                float  _EdgeWidth;
                float  _EdgeGlowIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
            CBUFFER_END

            #define MAX_CONTACTS 8

            float3 _ContactPositions[MAX_CONTACTS];
            float4 _ContactData[MAX_CONTACTS];
            float3 _PortalNormal;
            int    _ContactCount;

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += noise(p) * a; p *= 2.02; a *= 0.5;
                v += noise(p) * a; p *= 2.03; a *= 0.5;
                v += noise(p) * a; p *= 2.01; a *= 0.5;
                v += noise(p) * a;
                return v;
            }

            float3x3 GetPortalBasis()
            {
                float3 normal = normalize(_PortalNormal);
                float3 up = abs(normal.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 tangent = normalize(cross(up, normal));
                float3 bitangent = cross(normal, tangent);
                return float3x3(tangent, bitangent, normal);
            }

            float2 ProjectOntoPortal(float3 deltaWS, float3x3 basis)
            {
                float3 projected = mul(basis, deltaWS);
                return projected.xy;
            }

            float GetShapeDist(float2 delta, float shape, float radius, float shapeParam)
            {
                if (shape < 0.5)
                {
                    return length(delta) / max(radius, 0.001);
                }
                else if (shape < 1.5)
                {
                    float halfH = shapeParam * 0.5;
                    float2 scaledDelta = delta / max(radius, 0.001);
                    float scaledHalfH = halfH / max(radius, 0.001);
                    float bodyDist = length(max(abs(scaledDelta) - float2(0, scaledHalfH - 1.0), 0.0));
                    float capDist = length(scaledDelta - float2(0, scaledHalfH - 1.0));
                    return min(bodyDist, min(capDist, length(scaledDelta + float2(0, scaledHalfH - 1.0))));
                }
                else
                {
                    float aspect = shapeParam;
                    float2 halfSize = float2(aspect, 1.0);
                    float2 scaledDelta = delta / max(radius, 0.001);
                    float2 d = abs(scaledDelta) - halfSize;
                    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
                }
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 fragWS = input.positionWS;
                float3x3 basis = GetPortalBasis();
                float totalEdge = 0.0;

                for (int i = 0; i < MAX_CONTACTS; i++)
                {
                    if (i >= _ContactCount) break;

                    float ct = _ContactData[i].x;
                    float cr = _ContactData[i].y;
                    float cs = _ContactData[i].z;
                    float cp = _ContactData[i].w;
                    if (ct < 0.001) continue;

                    float2 delta = ProjectOntoPortal(fragWS - _ContactPositions[i], basis);
                    float angle = atan2(delta.y, delta.x);

                    float time = _Time.y * _EdgeNoiseSpeed;
                    float noiseVal = fbm(float2(angle * _EdgeNoiseScale, time));
                    float distortion = 1.0 + (noiseVal - 0.5) * 2.0 * _NoiseDistortion;
                    float dist = GetShapeDist(delta, cs, cr, cp) / distortion;
                    float scaledRadius = ct;

                    float cutoutMask = 1.0 - smoothstep(
                        scaledRadius - _CutoutSoftness,
                        scaledRadius + _CutoutSoftness,
                        dist);

                    float innerEdge = smoothstep(scaledRadius - _EdgeWidth, scaledRadius, dist);
                    float outerEdge = smoothstep(scaledRadius + _EdgeWidth, scaledRadius, dist);
                    float edgeMask = innerEdge * outerEdge * ct;

                    float pulse = 1.0 + sin(_Time.y * _PulseSpeed + dist * 10.0) * _PulseAmount;
                    edgeMask *= pulse;

                    totalEdge = max(totalEdge, edgeMask);
                }

                float3 glowColor = _GlowColor.rgb * totalEdge * _EdgeGlowIntensity * 0.5;

                float innerRing = 0.0;
                for (int j = 0; j < MAX_CONTACTS; j++)
                {
                    if (j >= _ContactCount) break;
                    float ct = _ContactData[j].x;
                    float cr = _ContactData[j].y;
                    float cs = _ContactData[j].z;
                    float cp = _ContactData[j].w;
                    if (ct < 0.001) continue;

                    float2 d = ProjectOntoPortal(fragWS - _ContactPositions[j], basis);
                    float dist = GetShapeDist(d, cs, cr, cp);
                    float scaledRadius = ct;

                    float softInner = smoothstep(scaledRadius * 0.3, 0.0, dist) * ct;
                    innerRing = max(innerRing, softInner);
                }

                glowColor += _GlowColor.rgb * innerRing * _EdgeGlowIntensity * 0.15;

                return half4(glowColor, totalEdge * 0.6);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _BumpMap_ST;
                float  _BumpScale;
                float  _Metallic;
                float  _Smoothness;
                float  _CutoutSoftness;
                float  _NoiseDistortion;
                float  _EdgeNoiseSpeed;
                float  _EdgeNoiseScale;
                float4 _GlowColor;
                float  _EdgeWidth;
                float  _EdgeGlowIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);

            #define MAX_CONTACTS 8

            float3 _ContactPositions[MAX_CONTACTS];
            float4 _ContactData[MAX_CONTACTS];
            float3 _PortalNormal;
            int    _ContactCount;

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += noise(p) * a; p *= 2.02; a *= 0.5;
                v += noise(p) * a; p *= 2.03; a *= 0.5;
                v += noise(p) * a; p *= 2.01; a *= 0.5;
                v += noise(p) * a;
                return v;
            }

            float3x3 GetPortalBasis()
            {
                float3 normal = normalize(_PortalNormal);
                float3 up = abs(normal.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 tangent = normalize(cross(up, normal));
                float3 bitangent = cross(normal, tangent);
                return float3x3(tangent, bitangent, normal);
            }

            float2 ProjectOntoPortal(float3 deltaWS, float3x3 basis)
            {
                float3 projected = mul(basis, deltaWS);
                return projected.xy;
            }

            float GetShapeDist(float2 delta, float shape, float radius, float shapeParam)
            {
                if (shape < 0.5)
                {
                    return length(delta) / max(radius, 0.001);
                }
                else if (shape < 1.5)
                {
                    float halfH = shapeParam * 0.5;
                    float2 scaledDelta = delta / max(radius, 0.001);
                    float scaledHalfH = halfH / max(radius, 0.001);
                    float bodyDist = length(max(abs(scaledDelta) - float2(0, scaledHalfH - 1.0), 0.0));
                    float capDist = length(scaledDelta - float2(0, scaledHalfH - 1.0));
                    return min(bodyDist, min(capDist, length(scaledDelta + float2(0, scaledHalfH - 1.0))));
                }
                else
                {
                    float aspect = shapeParam;
                    float2 halfSize = float2(aspect, 1.0);
                    float2 scaledDelta = delta / max(radius, 0.001);
                    float2 d = abs(scaledDelta) - halfSize;
                    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
                }
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs norInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posInputs.positionWS, norInputs.normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
