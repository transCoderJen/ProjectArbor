Shader "Custom/VolumetricFog"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.8, 0.85, 0.9, 1)
        _MaxDistance("Max Distance", Float) = 100
        _StepSize("Step Size", Range(0.1, 20)) = 0.5
        _DensityMultiplier("Density Multiplier", Range(0, 10)) = .025
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "Volumetric Fog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _FogColor;
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;

            float GetDensity()
            {
                return _DensityMultiplier;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                float depth = SampleSceneDepth(uv);
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = 0;
                float fogAccum = 0;

                while (distTravelled < distLimit)
                {
                    float density = GetDensity();

                    if (density > 0)
                    {
                        fogAccum += density * _StepSize;
                    }

                    distTravelled += _StepSize;
                }

                fogAccum = saturate(fogAccum);

                half3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, fogAccum);
                return half4(finalColor, 1);
                // return half4(fogAccum, fogAccum, fogAccum, 1);


            }
            ENDHLSL
        }
    }
}