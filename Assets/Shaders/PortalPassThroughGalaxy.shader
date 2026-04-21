Shader "Custom/PortalPassThroughGalaxy"
{
    Properties
    {
        [Header(Galaxy)]
        [HDR]_Galaxy_Color("Galaxy Color", Color) = (1, 1, 1, 1)
        _Galaxy("Galaxy Cubemap", CUBE) = "" {}
        [HDR]_Stars_Color("Stars Color", Color) = (1, 1, 1, 1)
        _Stars("Stars Cubemap", CUBE) = "" {}
        _Stars_Offset("Stars Reflection Offset", Vector) = (0.13, 0, 0, 0)

        [Header(Fresnel)]
        [HDR]_Fresnel_Color("Fresnel Color", Color) = (1, 1, 1, 1)
        _Fresnel_Power("Fresnel Power", Float) = 2.29
        _Fresnel_Scale("Noise Scale", Float) = 20.0
        _Fresnel_Speed("Fresnel Speed", Vector) = (0, 0, 0, 0)
        _Fresnel_Noise_Power("Noise Power", Float) = 1.0
        _FresnelTiling("Fresnel Noise Tiling", Vector) = (1, 1, 0, 0)
        _FresnelOffset("Fresnel Noise Offset", Vector) = (0, 0, 0, 0)

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
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PortalSurface"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
                float4 _Galaxy_Color;
                float4 _Stars_Color;
                float4 _Stars_Offset;
                float4 _Fresnel_Color;
                float  _Fresnel_Power;
                float  _Fresnel_Scale;
                float4 _Fresnel_Speed;
                float  _Fresnel_Noise_Power;
                float4 _FresnelTiling;
                float4 _FresnelOffset;
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

            TEXTURECUBE(_Galaxy);
            SAMPLER(sampler_Galaxy);
            TEXTURECUBE(_Stars);
            SAMPLER(sampler_Stars);

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

            float simpleNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = frac(sin(dot(i, float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
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

                float2 uv = input.uv;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float3 reflDirGalaxy = reflect(-viewDirWS, float3(0, 1, 0));
                float4 galaxySample = SAMPLE_TEXTURECUBE_LOD(_Galaxy, sampler_Galaxy, reflDirGalaxy, 0);
                float3 galaxy = _Galaxy_Color.rgb * galaxySample.rgb;

                float3 starsNormal = normalize(_Stars_Offset.xyz);
                float3 reflDirStars = reflect(-viewDirWS, starsNormal);
                float4 starsSample = SAMPLE_TEXTURECUBE_LOD(_Stars, sampler_Stars, reflDirStars, 0);
                float3 stars = _Stars_Color.rgb * starsSample.rgb;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _Fresnel_Power);

                float2 noiseUV = uv * _FresnelTiling.xy + _FresnelOffset.xy + _Time.y * _Fresnel_Speed.xy;
                float sn = simpleNoise(noiseUV * _Fresnel_Scale);
                float noisePowered = pow(sn, _Fresnel_Noise_Power);

                float3 fresnelEffect = _Fresnel_Color.rgb * fresnel * noisePowered;

                float3 surfaceColor = galaxy + stars + fresnelEffect;
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

                float3 finalColor = surfaceColor + _GlowColor.rgb * totalEdge * _EdgeGlowIntensity;
                float finalAlpha = saturate(surfaceAlpha + totalEdge * 0.8);

                return half4(finalColor, finalAlpha);
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
                float4 _Galaxy_Color;
                float4 _Stars_Color;
                float4 _Stars_Offset;
                float4 _Fresnel_Color;
                float  _Fresnel_Power;
                float  _Fresnel_Scale;
                float4 _Fresnel_Speed;
                float  _Fresnel_Noise_Power;
                float4 _FresnelTiling;
                float4 _FresnelOffset;
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
                float4 _Galaxy_Color;
                float4 _Stars_Color;
                float4 _Stars_Offset;
                float4 _Fresnel_Color;
                float  _Fresnel_Power;
                float  _Fresnel_Scale;
                float4 _Fresnel_Speed;
                float  _Fresnel_Noise_Power;
                float4 _FresnelTiling;
                float4 _FresnelOffset;
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

            TEXTURECUBE(_Galaxy);
            SAMPLER(sampler_Galaxy);
            TEXTURECUBE(_Stars);
            SAMPLER(sampler_Stars);

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
