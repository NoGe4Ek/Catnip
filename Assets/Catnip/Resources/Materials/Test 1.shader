Shader "Custom/HazeEffect"
{
    Properties
    {
        _HazeColor("Haze Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _HazeIntensity("Haze Intensity", Range(0, 2)) = 1.0
        _HazeDensity("Haze Density", Range(0, 1)) = 0.5
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 2.0
        _NoiseSpeed("Noise Speed", Range(0, 2)) = 0.5
        _NoiseIntensity("Noise Intensity", Range(0, 1)) = 0.3
        _EdgeSoftness("Edge Softness", Range(0, 1)) = 0.2
        _Turbulence("Turbulence", Range(0, 2)) = 0.5
        _PulseSpeed("Pulse Speed", Range(0, 3)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "HazeForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            // Properties
            float4 _HazeColor;
            float _HazeIntensity;
            float _HazeDensity;
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseIntensity;
            float _EdgeSoftness;
            float _Turbulence;
            float _PulseSpeed;

            // Шумовая функция
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Фрактальный шум для дымки
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }

            // Турбулентный шум
            float turbulence(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * abs(noise(p) - 0.5);
                    amplitude *= 0.5;
                    p *= 2.0;
                }
                
                return value;
            }

            // Маска прямоугольника с мягкими краями
            float rectangleMask(float2 uv, float softness)
            {
                float2 centered = uv * 2.0 - 1.0;
                float2 distance = 1.0 - abs(centered);
                float edge = smoothstep(0.0, softness, min(distance.x, distance.y));
                return edge;
            }

            // Пульсация
            float pulse(float time, float speed)
            {
                return (sin(time * speed) + 1.0) * 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.worldPos = vertexInput.positionWS;
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                
                // Базовый шум дымки
                float2 noiseUV = uv * _NoiseScale + time * _NoiseSpeed;
                float baseNoise = fbm(noiseUV);
                
                // Детализированный шум
                float detailNoise = fbm(uv * _NoiseScale * 2.0 + time * _NoiseSpeed * 1.3) * 0.3;
                
                // Турбулентность
                float turb = turbulence(uv * _NoiseScale * 0.8 + time * _NoiseSpeed * 0.7) * _Turbulence;
                
                // Комбинируем все шумы
                float combinedNoise = baseNoise * (1.0 + detailNoise) + turb;
                
                // Пульсация
                float pulseEffect = pulse(time, _PulseSpeed) * 0.2 + 0.8;
                
                // Форма дымки
                float hazeShape = combinedNoise * _NoiseIntensity * pulseEffect;
                
                // Маска квадрата с мягкими краями
                float mask = rectangleMask(uv, _EdgeSoftness);
                
                // Финальная альфа
                float finalAlpha = hazeShape * mask * _HazeIntensity * _HazeDensity;
                finalAlpha = saturate(finalAlpha);
                
                // Цвет с вариациями
                float3 finalColor = _HazeColor.rgb * (1.0 + baseNoise * 0.3);
                
                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}