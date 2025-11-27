Shader "Custom/URPUnlitDynamicBlur"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _BlurIntensity("Blur Intensity", Range(0, 10)) = 2.0
        _NoiseSpeed("Noise Speed", Float) = 0.5
        _NoiseScale("Noise Scale", Float) = 1.0
        _NoiseIntensity("Noise Intensity", Range(0, 0.1)) = 0.01
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

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
                
                // Four corners
                float a = simpleNoise(i);
                float b = simpleNoise(i + float2(1.0, 0.0));
                float c = simpleNoise(i + float2(0.0, 1.0));
                float d = simpleNoise(i + float2(1.0, 1.0));
                
                // Smooth interpolation
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }

            // Фрактальный шум для более интересного эффекта
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

            // Функция блюра с несколькими выборками
            float4 blurSample(Texture2D tex, SamplerState samplerTex, float2 uv, float blur)
            {
                float4 color = float4(0, 0, 0, 0);
                float2 texelSize = _MainTex_TexelSize.xy * blur;
                
                // 3x3 blur kernel
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        color += SAMPLE_TEXTURE2D_LOD(tex, samplerTex, uv + offset, 0);
                    }
                }
                
                return color / 9.0;
            }

            // Многослойный блюр с разной интенсивностью
            float4 multiLayerBlur(Texture2D tex, SamplerState samplerTex, float2 uv, float baseBlur, float noiseOffset)
            {
                float4 blur1 = blurSample(tex, samplerTex, uv + float2(noiseOffset, noiseOffset) * 0.1, baseBlur);
                float4 blur2 = blurSample(tex, samplerTex, uv + float2(-noiseOffset, noiseOffset) * 0.15, baseBlur * 1.2);
                float4 blur3 = blurSample(tex, samplerTex, uv + float2(noiseOffset, -noiseOffset) * 0.2, baseBlur * 0.8);
                
                return (blur1 + blur2 + blur3) / 3.0;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Получаем UV координаты экрана
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Генерируем динамический шум на основе времени
                float time = _Time.y * _NoiseSpeed;
                float noiseValue = fbm(screenUV * _NoiseScale + time);
                
                // Преобразуем шум в смещение (-1 до 1)
                float noiseOffset = (noiseValue * 2.0 - 1.0) * _NoiseIntensity;
                
                // Применяем многослойный блюр с шумовым смещением
                float4 blurredColor = multiLayerBlur(_MainTex, sampler_MainTex, screenUV, _BlurIntensity, noiseOffset);
                
                // Применяем цвет и возвращаем результат
                return blurredColor * _Color;
            }
            ENDHLSL
        }

        // Pass для глубины и нормалей (опционально)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}