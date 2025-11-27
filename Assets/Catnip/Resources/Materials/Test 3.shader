Shader "Custom/LiquidNitrogenFog"
{
    Properties
    {
        // Цвета азотного тумана
        _FogColor("Fog Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _CoreColor("Core Color", Color) = (0.7, 0.8, 0.9, 1.0)
        _EdgeColor("Edge Color", Color) = (1.0, 1.0, 1.0, 1.0)
        
        // Физические свойства
        _Density("Density", Range(0, 2)) = 1.2
        _Viscosity("Viscosity", Range(0.5, 5)) = 2.0
        _GravityEffect("Gravity Effect", Range(0, 1)) = 0.7
        _SurfaceTension("Surface Tension", Range(0, 3)) = 1.5
        
        // Движение жидкости
        _FlowSpeed("Flow Speed", Range(0, 1)) = 0.3
        _SwirlIntensity("Swirl Intensity", Range(0, 2)) = 0.8
        _WaveFrequency("Wave Frequency", Range(0, 10)) = 4.0
        
        // Детализация
        _TurbulenceScale("Turbulence Scale", Range(1, 20)) = 8.0
        _DetailIntensity("Detail Intensity", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+200" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "NitrogenMain"
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
            float4 _CoreColor;
            float4 _EdgeColor;
            float _Density;
            float _Viscosity;
            float _GravityEffect;
            float _SurfaceTension;
            float _FlowSpeed;
            float _SwirlIntensity;
            float _WaveFrequency;
            float _TurbulenceScale;
            float _DetailIntensity;

            // Перлин-шум для органичных форм
            float perlinNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = dot(i, float2(127.1, 311.7));
                float b = dot(i + float2(1.0, 0.0), float2(127.1, 311.7));
                float c = dot(i + float2(0.0, 1.0), float2(127.1, 311.7));
                float d = dot(i + float2(1.0, 1.0), float2(127.1, 311.7));
                
                a = frac(sin(a) * 43758.5453);
                b = frac(sin(b) * 43758.5453);
                c = frac(sin(c) * 43758.5453);
                d = frac(sin(d) * 43758.5453);
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Фрактальный шум для турбулентности
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * perlinNoise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            // Вихревое течение как у жидкого азота
            float2 vortexFlow(float2 uv, float time)
            {
                float2 center = float2(0.5, 0.3); // Смещенный центр для гравитации
                float2 toCenter = uv - center;
                float dist = length(toCenter);
                
                // Вращение с вязкостью
                float angle = dist * 6.0 - time * _FlowSpeed * 2.0;
                float viscosityEffect = exp(-dist * _Viscosity);
                angle *= viscosityEffect;
                
                float2 rotated = float2(
                    toCenter.x * cos(angle) - toCenter.y * sin(angle),
                    toCenter.x * sin(angle) + toCenter.y * cos(angle)
                );
                
                return rotated + center;
            }

            // Гравитационное оседание
            float gravityEffect(float2 uv, float time)
            {
                // Туман оседает вниз
                float gravity = (1.0 - uv.y) * _GravityEffect;
                
                // Волны на поверхности
                float surfaceWave = sin(uv.x * _WaveFrequency + time * _FlowSpeed) * 0.1;
                gravity += surfaceWave * (1.0 - uv.y);
                
                return gravity;
            }

            // Поверхностное натяжение
            float surfaceTension(float2 uv, float density)
            {
                // Краевые эффекты - туман собирается в капли
                float2 gradient = float2(ddx(density), ddy(density));
                float edge = length(gradient) * _SurfaceTension;
                
                // Сферизация по краям
                float2 centered = uv - 0.5;
                float roundness = 1.0 - length(centered) * 2.0;
                edge *= saturate(roundness + 0.5);
                
                return edge;
            }

            // Основная плотность тумана
            float fogDensity(float2 uv, float time)
            {
                // Вихревое движение
                float2 flowUV = vortexFlow(uv, time);
                
                // Турбулентность
                float turbulence = fbm(flowUV * _TurbulenceScale, 3) * _SwirlIntensity;
                
                // Гравитация
                float gravity = gravityEffect(uv, time);
                
                // Базовая форма - тяжелый туман снизу
                float baseDensity = (1.0 - uv.y) + gravity;
                baseDensity = saturate(baseDensity * 2.0);
                
                // Добавляем турбулентность
                float density = baseDensity + (turbulence - 0.5) * 0.4;
                
                // Детализация
                float details = fbm(uv * 15.0, 2) * _DetailIntensity;
                density += details * 0.2;
                
                return saturate(density);
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
                
                // Основная плотность
                float density = fogDensity(uv, time);
                density *= _Density;
                
                // Поверхностное натяжение
                float tension = surfaceTension(uv, density);
                density += tension * 0.3;
                
                // Цветовая схема жидкого азота
                float3 color = _FogColor.rgb;
                
                // Ядро - более насыщенный цвет
                float coreMask = smoothstep(0.3, 0.7, density);
                color = lerp(color, _CoreColor.rgb, coreMask);
                
                // Края с поверхностным натяжением - яркие
                float edgeMask = tension * 2.0;
                color = lerp(color, _EdgeColor.rgb, edgeMask);
                
                // Мерцание как у испаряющегося азота
                float shimmer = fbm(uv * 20.0 + time, 2) * 0.1;
                color += shimmer;
                
                // Финальная плотность с порогом
                float alpha = smoothstep(0.1, 0.8, density);
                
                return float4(color, alpha);
            }
            ENDHLSL
        }

        // Пасс для глубины и объема
        Pass
        {
            Name "NitrogenVolume"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragVolume

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

            float4 _CoreColor;
            float _Density;
            float _Viscosity;
            float _FlowSpeed;

            float perlinNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = dot(i, float2(127.1, 311.7)); a = frac(sin(a) * 43758.5453);
                float b = dot(i + float2(1.0, 0.0), float2(127.1, 311.7)); b = frac(sin(b) * 43758.5453);
                float c = dot(i + float2(0.0, 1.0), float2(127.1, 311.7)); c = frac(sin(c) * 43758.5453);
                float d = dot(i + float2(1.0, 1.0), float2(127.1, 311.7)); d = frac(sin(d) * 43758.5453);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            float4 fragVolume(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                
                // Только глубокие, вязкие области
                float depth = (1.0 - uv.y) * 1.5;
                depth = saturate(depth);
                
                // Вязкое движение глубинных слоев
                float2 vortexUV = input.uv;
                vortexUV.x += sin(time * _FlowSpeed * 0.5) * 0.1;
                float deepTurbulence = perlinNoise(vortexUV * 5.0 + time * 0.3) * 0.2;
                
                depth += deepTurbulence;
                depth *= _Density * 0.5;
                
                // Только самые плотные области
                depth = smoothstep(0.4, 0.8, depth);
                
                return float4(_CoreColor.rgb, depth * 0.3);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}