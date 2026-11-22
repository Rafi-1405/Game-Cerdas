Shader "ZombieUnity/Night Sky"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.004, 0.008, 0.022, 1)
        _Horizon("Horizon", Color) = (0.024, 0.032, 0.052, 1)
        _MoonDirection("Moon direction", Vector) = (0.35, 0.7, 0.5, 0)
        _Exposure("Exposure", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Zenith;
                float4 _Horizon;
                float4 _MoonDirection;
                float _Exposure;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash21(i), hash21(i + float2(1.0, 0.0)), f.x),
                    lerp(hash21(i + float2(0.0, 1.0)), hash21(i + float2(1.0, 1.0)), f.x),
                    f.y
                );
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float y = saturate(d.y);
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, pow(y, 0.48));

                float2 uv = float2(atan2(d.x, d.z), asin(clamp(d.y, -1.0, 1.0)));
                float2 st = uv * float2(165.0, 200.0);
                float2 cell = floor(st);
                float n = hash21(cell);
                float2 center = float2(hash21(cell + 3.7), hash21(cell + 8.2));
                float star = (1.0 - smoothstep(0.012, 0.075, length(frac(st) - center))) * step(0.996, n) * smoothstep(0.02, 0.2, y);
                sky += star * lerp(0.16, 0.6, hash21(cell + 9.5)) * float3(0.76, 0.85, 1.0);

                float3 moon = normalize(_MoonDirection.xyz);
                float alignment = dot(d, moon);
                float disc = smoothstep(0.99989, 0.99992, alignment);
                float glow = pow(saturate(alignment), 600.0) * 0.025;
                float craters = noise2D(uv * 650.0) * 0.17 + noise2D(uv * 1300.0) * 0.08;
                sky += float3(0.62, 0.72, 0.85) * (disc * (0.65 + craters) + glow);

                float cloud = noise2D(uv * float2(5.0, 12.0)) * 0.6 + noise2D(uv * float2(13.0, 28.0)) * 0.25 + noise2D(uv * float2(30.0, 55.0)) * 0.15;
                float mask = smoothstep(0.48, 0.72, cloud) * smoothstep(-0.05, 0.12, d.y) * 0.65;
                sky = lerp(sky, float3(0.012, 0.017, 0.028), mask);

                return half4(sky * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
