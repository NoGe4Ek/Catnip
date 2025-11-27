Shader "Custom/URPUnlitDynamicBlurWithEdges"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _BlurIntensity("Blur Intensity", Range(0, 10)) = 2.0
        _NoiseSpeed("Noise Speed", Float) = 0.5
        _NoiseScale("Noise Scale", Float) = 1.0
        _NoiseIntensity("Noise Intensity", Range(0, 0.1)) = 0.01
        _EdgeNoiseScale("Edge Noise Scale", Float) = 5.0
        _EdgeNoiseIntensity("Edge Noise Intensity", Range(0, 1)) = 0.3
        _EdgeSmoothness("Edge Smoothness", Range(0, 1)) = 0.2
        _EdgeWidth("Edge Width", Range(0, 0.5)) = 0.1
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
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
                float4 screenPos : TEXCOORD1;
                float2 localPos : TEXCOORD2;
            };

            // Texture properties
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            // Shader properties
            float _BlurIntensity;
            float _NoiseSpeed;
            float _NoiseScale;
            float _NoiseIntensity;
            float _EdgeNoiseScale;
            float _EdgeNoiseIntensity;
            float _EdgeSmoothness;
            float _EdgeWidth;
            float4 _Color;

            // Простая функция шума
            float simpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Улучшенный шум
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                float a = simpleNoise(i);
                float b = simpleNoise(i + float2(1.0, 0.0));
                float c = simpleNoise(i + float2(0.0, 1.0));
                float d = simpleNoise(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }

            // Фрактальный шум
            float fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(uv * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }

            // Функция для создания шумовых границ
            float getEdgeAlpha(float2 uv, float2 localPos, float time)
            {
                // Базовое расстояние до краев (0 в центре, 1 на краях)
                float2 centeredUV = localPos * 2.0 - 1.0; // -1 до 1
                float distanceToEdge = 1.0 - max(abs(centeredUV.x), abs(centeredUV.y));
                
                // Добавляем шум к границам
                float edgeNoise = fbm(uv * _EdgeNoiseScale + time);
                float noisyDistance = distanceToEdge + (edgeNoise - 0.5) * _EdgeNoiseIntensity;
                
                // Плавное затухание с шумом
                float edgeAlpha = smoothstep(0.0, _EdgeSmoothness, noisyDistance / _EdgeWidth);
                
                return saturate(edgeAlpha);
            }

            // Функция блюра с шумовым смещением
            float4 blurSampleWithNoise(Texture2D tex, SamplerState samplerTex, float2 uv, float blur, float time)
            {
                float4 color = float4(0, 0, 0, 0);
                float2 texelSize = _MainTex_TexelSize.xy * blur;
                float totalWeight = 0.0;
                
                // 5x5 blur kernel с шумовыми смещениями
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        // Добавляем шум к смещениям
                        float2 noiseOffset = float2(
                            fbm(uv * 3.0 + time + x * 0.1),
                            fbm(uv * 3.0 + time + y * 0.1 + 10.0)
                        ) * _NoiseIntensity;
                        
                        float2 sampleOffset = float2(x, y) * texelSize + noiseOffset;
                        float weight = 1.0 / (1.0 + length(float2(x, y)));
                        
                        color += SAMPLE_TEXTURE2D_LOD(tex, samplerTex, uv + sampleOffset, 0) * weight;
                        totalWeight += weight;
                    }
                }
                
                return color / totalWeight;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.localPos = input.uv; // Используем UV как локальные координаты
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float time = _Time.y * _NoiseSpeed;
                
                // Получаем альфа-канал для размытых границ с шумом
                float edgeAlpha = getEdgeAlpha(input.uv, input.localPos, time);
                
                // Применяем блюр с шумом
                float4 blurredColor = blurSampleWithNoise(_MainTex, sampler_MainTex, screenUV, _BlurIntensity, time);
                
                // Комбинируем цвет с альфа-каналом границ
                float4 finalColor = blurredColor * _Color;
                finalColor.a *= edgeAlpha;
                
                return finalColor;
            }
            ENDHLSL
        }

        // Альтернативная версия с круговыми границами
        Pass
        {
            Name "CircularEdges"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragCircular

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
                float4 screenPos : TEXCOORD1;
                float2 localPos : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _BlurIntensity;
            float _NoiseSpeed;
            float _NoiseScale;
            float _NoiseIntensity;
            float _EdgeNoiseScale;
            float _EdgeNoiseIntensity;
            float _EdgeSmoothness;
            float _EdgeWidth;
            float4 _Color;

            float simpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                float a = simpleNoise(i);
                float b = simpleNoise(i + float2(1.0, 0.0));
                float c = simpleNoise(i + float2(0.0, 1.0));
                float d = simpleNoise(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(uv * pow(2.0, i));
                    amplitude *= 0.5;
                }
                
                return value;
            }

            // Круговые границы с шумом
            float getCircularEdgeAlpha(float2 localPos, float time)
            {
                // Расстояние от центра
                float2 centered = localPos * 2.0 - 1.0;
                float distanceFromCenter = length(centered);
                
                // Добавляем шум к радиусу
                float angle = atan2(centered.y, centered.x);
                float radialNoise = fbm(float2(angle * 2.0, time * 0.5)) * _EdgeNoiseIntensity;
                
                float noisyRadius = distanceFromCenter + radialNoise;
                
                // Плавное затухание от центра к краям
                float innerEdge = 0.5 - _EdgeWidth;
                float outerEdge = 0.5 + _EdgeWidth;
                
                float alpha = 1.0 - smoothstep(innerEdge, outerEdge, noisyRadius);
                return alpha;
            }

            float4 blurSampleWithNoise(Texture2D tex, SamplerState samplerTex, float2 uv, float blur, float time)
            {
                float4 color = float4(0, 0, 0, 0);
                float2 texelSize = _MainTex_TexelSize.xy * blur;
                float totalWeight = 0.0;
                
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 noiseOffset = float2(
                            fbm(uv * 2.0 + time + x),
                            fbm(uv * 2.0 + time + y + 5.0)
                        ) * _NoiseIntensity;
                        
                        float2 sampleOffset = float2(x, y) * texelSize + noiseOffset;
                        float weight = 1.0 / (1.0 + length(float2(x, y)));
                        
                        color += SAMPLE_TEXTURE2D_LOD(tex, samplerTex, uv + sampleOffset, 0) * weight;
                        totalWeight += weight;
                    }
                }
                
                return color / totalWeight;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.localPos = input.uv;
                return output;
            }

            float4 fragCircular(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float time = _Time.y * _NoiseSpeed;
                
                // Круговые границы с шумом
                float edgeAlpha = getCircularEdgeAlpha(input.localPos, time);
                
                // Блюр с шумом
                float4 blurredColor = blurSampleWithNoise(_MainTex, sampler_MainTex, screenUV, _BlurIntensity, time);
                
                float4 finalColor = blurredColor * _Color;
                finalColor.a *= edgeAlpha;
                
                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}