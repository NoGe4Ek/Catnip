Shader "Custom/ToonBossFog"
{
    Properties
    {
        // Основные цвета
        _FogColor("Fog Color", Color) = (0.4, 0.3, 0.5, 1.0)
        _DarkColor("Dark Color", Color) = (0.2, 0.15, 0.25, 1.0)
        _BrightColor("Bright Color", Color) = (0.7, 0.6, 0.9, 1.0)
        
        // Плотность и форма
        _Density("Density", Range(0, 1)) = 0.8
        _Thickness("Thickness", Range(0.1, 3)) = 1.5
        _EdgeHardness("Edge Hardness", Range(1, 10)) = 4.0
        
        // Движение тумана
        _SwirlSpeed("Swirl Speed", Range(0, 2)) = 0.8
        _ChaosSpeed("Chaos Speed", Range(0, 3)) = 1.5
        _PulseSpeed("Pulse Speed", Range(0, 1)) = 0.3
        
        // Toon-эффекты
        _ToonLevels("Toon Levels", Range(2, 8)) = 4
        _Contrast("Contrast", Range(1, 5)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FogMain"
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
            float4 _FogColor;
            float4 _DarkColor;
            float4 _BrightColor;
            float _Density;
            float _Thickness;
            float _EdgeHardness;
            float _SwirlSpeed;
            float _ChaosSpeed;
            float _PulseSpeed;
            float _ToonLevels;
            float _Contrast;

            // Хаотичный шум для вязкого движения
            float chaoticNoise(float2 uv, float time)
            {
                // Многомасштабный шум для сложного движения
                float2 p1 = uv * 2.0 + time * 0.7;
                float2 p2 = uv * 4.0 - time * 1.2;
                float2 p3 = uv * 8.0 + time * 0.9;
                
                float n1 = frac(sin(dot(p1, float2(127.1, 311.7))) * 43758.5453);
                float n2 = frac(sin(dot(p2, float2(269.5, 183.3))) * 43758.5453);
                float n3 = frac(sin(dot(p3, float2(419.2, 371.9))) * 43758.5453);
                
                return (n1 + n2 * 0.5 + n3 * 0.25) / 1.75;
            }

// Вихревой шум для закручивания
            float2 vortexUV1(float2 uv, float time)
            {
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                float dist = length(toCenter);
                
                // Вращение
                float angle = dist * 8.0 - time * _SwirlSpeed;
                float2 rotated = float2(
                    toCenter.x * cos(angle) - toCenter.y * sin(angle),
                    toCenter.x * sin(angle) + toCenter.y * cos(angle)
                );
                
                return rotated + center;
            }
            
            // Основная форма тумана
            float fogDensity(float2 uv, float time)
            {
                // Базовый круг
                float2 centered = uv - 0.5;
                float baseCircle = 1.0 - length(centered) * 2.0;
                baseCircle = saturate(baseCircle * _Thickness);
                
                // Вихревое искажение
                float2 vortexUV = vortexUV1(uv, time);
                float vortexNoise = chaoticNoise(vortexUV, time * 0.5);
                
                // Хаотичное движение
                float chaos1 = chaoticNoise(uv + time * _ChaosSpeed, time);
                float chaos2 = chaoticNoise(uv - time * _ChaosSpeed * 0.7, time + 10.0);
                
                // Комбинируем все влияния
                float combined = baseCircle;
                combined += (vortexNoise - 0.5) * 0.4;
                combined += (chaos1 - 0.5) * 0.3;
                combined += (chaos2 - 0.5) * 0.2;
                
                // Пульсация
                float pulse = sin(time * _PulseSpeed) * 0.1 + 1.0;
                combined *= pulse;
                
                return saturate(combined);
            }

            

            // Toon-квантование
            float toonify(float value, float levels)
            {
                float stepped = floor(value * levels) / levels;
                return stepped;
            }

            // Создание контрастных слоев тумана
            float3 calculateToonFog(float density, float2 uv, float time)
            {
                // Квантуем плотность для toon-эффекта
                float toonDensity = toonify(density, _ToonLevels);
                
                // Усиливаем контраст
                toonDensity = pow(toonDensity, _Contrast);
                
                // Создаем слои с разными цветами
                float3 fog = _FogColor.rgb;
                
                // Темные области
                float darkMask = step(toonDensity, 0.3);
                fog = lerp(fog, _DarkColor.rgb, darkMask);
                
                // Яркие края
                float brightMask = step(0.7, toonDensity);
                fog = lerp(fog, _BrightColor.rgb, brightMask);
                
                // Добавляем шум для текстуры
                float textureNoise = chaoticNoise(uv * 10.0, time * 2.0) * 0.1;
                fog += textureNoise;
                
                return fog;
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
                
                // Плотность тумана
                float density = fogDensity(uv, time);
                
                // Применяем общую плотность
                density *= _Density;
                
                // Резкие края
                density = pow(density, _EdgeHardness);
                
                // Toon-версия тумана
                float3 toonFog = calculateToonFog(density, uv, time);
                
                // Финальный альфа
                float alpha = density;
                
                return float4(toonFog, alpha);
            }
            ENDHLSL
        }

        // Дополнительный пасс для глубины и объема
        Pass
        {
            Name "FogDepth"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth

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
            };

            float4 _DarkColor;
            float _Density;
            float _SwirlSpeed;
            float _ChaosSpeed;

            float chaoticNoise(float2 uv, float time)
            {
                float2 p1 = uv * 3.0 + time;
                float2 p2 = uv * 6.0 - time * 1.5;
                float n1 = frac(sin(dot(p1, float2(127.1, 311.7))) * 43758.5453);
                float n2 = frac(sin(dot(p2, float2(269.5, 183.3))) * 43758.5453);
                return (n1 + n2 * 0.7) / 1.7;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            float4 fragDepth(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                
                // Только глубокие, темные области
                float2 centered = uv - 0.5;
                float depth = 1.0 - length(centered) * 2.5;
                depth = saturate(depth);
                
                // Добавляем движение к глубине
                float depthNoise = chaoticNoise(uv, time * _ChaosSpeed) * 0.3;
                depth += depthNoise;
                
                // Только самые темные части
                depth = step(0.5, depth) * depth * _Density * 0.3;
                
                return float4(_DarkColor.rgb, depth);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}