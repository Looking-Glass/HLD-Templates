Shader "Custom/URP/PlanarReflectionBlurred"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _ReflectionOpacity ("Reflection Opacity", Range(0, 1)) = 0.5
        _ReflectionBrightness ("Reflection Brightness", Range(0, 3)) = 1.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 3.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        
        [Header(Blur Settings)]
        _MaxBlurRadius ("Max Blur Radius", Range(0, 0.05)) = 0.02
        _BlurSamples ("Blur Quality", Range(1, 4)) = 2
        
        [Header(Distance Falloff)]
        _DistanceFalloff ("Distance Falloff", Range(0, 2)) = 0.8
        _FalloffStart ("Falloff Start", Range(0, 1)) = 0.0
        _FalloffPower ("Falloff Curve", Range(0.5, 4)) = 1.5
        _FadeEnd ("Fade Out End", Range(0, 1)) = 0.85
        _DepthScale ("Depth Scale", Range(0.01, 1)) = 0.1
        
        [Header(Reflection Tint)]
        _TintStrength ("Tint Strength", Range(0, 1)) = 1.0
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 positionWSAndFogFactor : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ReflectionTex_TexelSize;
                float _ReflectionStrength;
                float _ReflectionOpacity;
                float _ReflectionBrightness;
                float _Smoothness;
                float _Metallic;
                float _FresnelPower;
                float _BumpScale;
                float _MaxBlurRadius;
                float _BlurSamples;
                float _DistanceFalloff;
                float _FalloffStart;
                float _FalloffPower;
                float _FadeEnd;
                float _DepthScale;
                float _TintStrength;
            CBUFFER_END

            half3 TintReflection(half3 reflection, half3 tint, half strength)
            {
                half luma = dot(reflection, half3(0.2126, 0.7152, 0.0722));
                half tintLuma = dot(tint, half3(0.2126, 0.7152, 0.0722));
                half3 normalizedTint = tintLuma > 0.001 ? tint / tintLuma : half3(1, 1, 1);
                half3 tinted = (1.0 - 2.0 * tint) * reflection * reflection + 2.0 * tint * reflection;
                return lerp(reflection, tinted, strength);
            }

            half GaussianWeight(half x, half sigma)
            {
                return exp(-0.5 * (x * x) / (sigma * sigma));
            }

            // Calculate distance-based blur factor using view-space depth
            // This works regardless of camera angle
            half CalculateDistanceBlur(float viewDepth, half baseRoughness)
            {
                // Normalize depth using configurable scale
                half normalizedDepth = saturate(viewDepth * _DepthScale);
                
                // Remap to start after FalloffStart threshold
                half remappedDist = saturate((normalizedDepth - _FalloffStart) / (1.0 - _FalloffStart));
                
                // Apply power curve
                half falloffFactor = pow(remappedDist, _FalloffPower);
                
                // Combine base roughness with distance-based blur
                half effectiveRoughness = baseRoughness + falloffFactor * _DistanceFalloff;
                
                return saturate(effectiveRoughness);
            }

            // Calculate fade-out using view-space depth
            half CalculateDistanceFade(float viewDepth)
            {
                // Normalize depth using configurable scale
                half normalizedDepth = saturate(viewDepth * _DepthScale);
                
                // Fade out based on depth
                half fadeStart = _FadeEnd * 0.5;
                half fade = 1.0 - saturate((normalizedDepth - fadeStart) / (max(0.001, _FadeEnd - fadeStart)));
                
                return fade * fade;
            }

            half3 SampleReflectionBlurred(float2 uv, half roughness)
            {
                // No blur for perfectly smooth surfaces
                if (roughness < 0.01)
                {
                    return SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv).rgb;
                }

                // Blur radius scales with roughness squared
                half blurRadius = roughness * roughness * _MaxBlurRadius;
                half sigma = blurRadius / 3.0;
                
                int quality = (int)_BlurSamples;
                
                half3 colorSum = 0;
                half weightSum = 0;
                
                // Poisson disc for good distribution
                static const float2 poissonDisc[16] = {
                    float2(-0.94201624, -0.39906216),
                    float2(0.94558609, -0.76890725),
                    float2(-0.09418410, -0.92938870),
                    float2(0.34495938, 0.29387760),
                    float2(-0.91588581, 0.45771432),
                    float2(-0.81544232, -0.87912464),
                    float2(-0.38277543, 0.27676845),
                    float2(0.97484398, 0.75648379),
                    float2(0.44323325, -0.97511554),
                    float2(0.53742981, -0.47373420),
                    float2(-0.26496911, -0.41893023),
                    float2(0.79197514, 0.19090188),
                    float2(-0.24188840, 0.99706507),
                    float2(-0.81409955, 0.91437590),
                    float2(0.19984126, 0.78641367),
                    float2(0.14383161, -0.14100790)
                };

                int sampleCount = quality * 4;
                
                // Center sample
                half3 centerColor = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv).rgb;
                half centerWeight = GaussianWeight(0, sigma);
                colorSum += centerColor * centerWeight;
                weightSum += centerWeight;
                
                // Disc samples
                for (int i = 0; i < sampleCount; i++)
                {
                    float2 offset = poissonDisc[i] * blurRadius;
                    float2 sampleUV = uv + offset;
                    
                    half dist = length(offset);
                    half weight = GaussianWeight(dist, sigma);
                    
                    half3 sampleColor = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, sampleUV).rgb;
                    colorSum += sampleColor * weight;
                    weightSum += weight;
                }
                
                return colorSum / weightSum;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                float3 positionWS = positionInputs.positionWS;
                output.positionWSAndFogFactor = float4(positionWS, ComputeFogFactor(positionInputs.positionCS.z));
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.screenPos = ComputeScreenPos(positionInputs.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Screen UV for reflection sampling
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // View-space depth (distance from camera along view direction)
                float viewDepth = length(input.positionWSAndFogFactor.xyz - _WorldSpaceCameraPos);
                
                // Base roughness from smoothness
                half baseRoughness = 1.0 - _Smoothness;
                
                // Calculate distance-based effective roughness using view depth
                half effectiveRoughness = CalculateDistanceBlur(viewDepth, baseRoughness);
                
                // Calculate distance fade using view depth
                half distanceFade = CalculateDistanceFade(viewDepth);
                
                // Sample blurred reflection with distance-based roughness
                half3 reflection = SampleReflectionBlurred(screenUV, effectiveRoughness);
                
                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseColor = baseMap.rgb * _BaseColor.rgb;
                
                // Apply luminance-preserving tint
                half3 tintedReflection = TintReflection(reflection, baseColor, _TintStrength);

                // Normal mapping
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // View direction and fresnel
                half3 viewDirWS = normalize(input.viewDirWS);
                half NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);

                // PBR lighting setup
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWSAndFogFactor.xyz;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.fogCoord = input.positionWSAndFogFactor.w;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.alpha = 1.0;

                // Lit base surface
                half4 litBase = UniversalFragmentPBR(inputData, surfaceData);
                
                // Fresnel-boosted reflection strength
                half reflectionMask = lerp(_ReflectionStrength, 1.0, fresnel);
                
                // Blend reflection with base color (not dark PBR result)
                half3 reflectionBlend = lerp(baseColor, tintedReflection, reflectionMask * _ReflectionOpacity);
                
                // Combine with lighting - additive blend with brightness control
                half3 finalColor = litBase.rgb + reflectionBlend * distanceFade * _ReflectionBrightness;

                return half4(finalColor, 1.0);
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

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

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
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
