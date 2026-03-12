Shader "Custom/URP/EnergyShieldPlane_FullPlane"
{
    Properties
    {
        [HDR]_BaseColor("Base Color", Color) = (0.35, 0.75, 1.5, 1)
        [HDR]_LineColor("Line Color", Color) = (0.8, 0.95, 1.8, 1)

        _Opacity("Opacity", Range(0,1)) = 0.6

        _CellScaleA("Cell Scale A", Float) = 14
        _CellScaleB("Cell Scale B", Float) = 28
        _LineWidth("Line Width", Range(0.001,0.2)) = 0.05
        _LineIntensity("Line Intensity", Range(0,8)) = 2.5

        _FlowSpeed("Flow Speed", Float) = 0.4
        _DistortStrength("Distort Strength", Range(0,0.2)) = 0.03
        _PulseSpeed("Pulse Speed", Float) = 1.5
        _PulseAmount("Pulse Amount", Range(0,2)) = 0.35

        _NoiseScale("Noise Scale", Float) = 6
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _LineColor;

                float _Opacity;

                float _CellScaleA;
                float _CellScaleB;
                float _LineWidth;
                float _LineIntensity;

                float _FlowSpeed;
                float _DistortStrength;
                float _PulseSpeed;
                float _PulseAmount;

                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f);

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

            float voronoiLines(float2 uv, float scale, float width, float t)
            {
                uv *= scale;

                float2 g = floor(uv);
                float2 f = frac(uv);

                float d1 = 999.0;
                float d2 = 999.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 o = float2(x, y);
                        float2 h = hash22(g + o);
                        float2 p = o + 0.5 + 0.35 * sin(t + 6.2831 * h);
                        float d = length(f - p);

                        if (d < d1)
                        {
                            d2 = d1;
                            d1 = d;
                        }
                        else if (d < d2)
                        {
                            d2 = d;
                        }
                    }
                }

                float edge = d2 - d1;
                return 1.0 - smoothstep(0.0, width, edge);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y * _FlowSpeed;

                float2 centered = uv * 2.0 - 1.0;

                float2 flow =
                    (float2(
                        fbm(centered * _NoiseScale + float2(0.0, t * 0.7)),
                        fbm(centered * _NoiseScale + float2(7.1, -t * 0.5))
                    ) - 0.5) * _DistortStrength;

                float2 uvd = uv + flow;

                float lineA = voronoiLines(uvd + float2(t * 0.18, -t * 0.11), _CellScaleA, _LineWidth, t);
                float lineB = voronoiLines(uvd * 1.13 + float2(-t * 0.12, t * 0.16), _CellScaleB, _LineWidth * 0.55, -t * 1.3);

                float crackle = max(lineA, lineB * 0.8);

                float softNoise = lerp(1.0, fbm(uvd * _NoiseScale * 0.8 + t), _NoiseStrength);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + (uv.x + uv.y) * 10.0) * _PulseAmount;

                float fill  = 0.10 * softNoise;
                float lines = crackle * _LineIntensity * pulse * softNoise;

                float3 col = _BaseColor.rgb * fill + _LineColor.rgb * lines;
                float alpha = saturate(fill * 0.4 + crackle) * _Opacity;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}