Shader "Custom/PortalPassThrough"
{
    Properties
    {
        [Header(Surface)]
        [HDR]_BaseColor("Surface Color", Color) = (0.2, 0.5, 1.0, 1)
        _SurfaceOpacity("Surface Opacity", Range(0, 1)) = 0.4
        _SurfaceNoiseScale("Surface Noise Scale", Float) = 4.0
        _FlowSpeed("Flow Speed", Float) = 0.3

        [Header(Cutout)]
        _CutoutSoftness("Cutout Softness", Range(0.01, 0.5)) = 0.1
        _NoiseDistortion("Edge Noise Distortion", Range(0, 0.5)) = 0.15
        _EdgeNoiseSpeed("Edge Noise Speed", Float) = 2.0
        _EdgeNoiseScale("Edge Noise Scale", Float) = 3.0

        [Header(Glow)]
        [HDR]_GlowColor("Glow Color", Color) = (0.5, 1.0, 2.0, 1)
        _EdgeWidth("Edge Width (World Units)", Range(0.01, 0.5)) = 0.12
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
                float4 _BaseColor;
                float  _SurfaceOpacity;
                float  _SurfaceNoiseScale;
                float  _FlowSpeed;

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
            #define SHAPE_SPHERE 0.0
            #define SHAPE_CAPSULE 1.0
            #define SHAPE_BOX     2.0

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

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                float3 fragWS = input.positionWS;
                float t = _Time.y * _FlowSpeed;

                float2 centered = uv * 2.0 - 1.0;
                float2 flow = (float2(
                    fbm(centered * _SurfaceNoiseScale + float2(0, t * 0.7)),
                    fbm(centered * _SurfaceNoiseScale + float2(7.1, -t * 0.5))
                ) - 0.5) * 0.03;
                float2 uvd = uv + flow;

                float pattern = fbm(uvd * _SurfaceNoiseScale * 0.8 + t * 0.5);
                float surfaceAlpha = pattern * 0.3 * _SurfaceOpacity;
                float3 surfaceColor = _BaseColor.rgb * (0.5 + pattern * 0.5);

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
                    ComputeCutout(fragWS, _ContactPositions[i], contactRadius, contactShape, contactParam, contactTransition, cutoutMask, edgeMask);

                    totalCutout = max(totalCutout, cutoutMask);
                    totalEdge = max(totalEdge, edgeMask);
                }

                surfaceAlpha = surfaceAlpha * (1.0 - totalCutout);

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
                float4 _BaseColor;
                float  _SurfaceOpacity;
                float  _SurfaceNoiseScale;
                float  _FlowSpeed;

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
            #define SHAPE_SPHERE 0.0
            #define SHAPE_CAPSULE 1.0
            #define SHAPE_BOX     2.0

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
    }
}
