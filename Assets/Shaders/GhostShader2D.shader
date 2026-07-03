Shader "Shader Graphs/Ghost 2D"
{
    Properties
    {
        [NoScaleOffset]_MainTex("Sprite Texture", 2D) = "white" {}
        [HDR]_Tint("Tint", Color) = (0.2, 0.65, 1, 1)

        _ScrollSpeed("Scroll Speed", Float) = -1
        _LineThickness("Line Thickness", Range(0, 1)) = 0.5
        _LineRotation("Line Rotation", Range(0, 360)) = 90
        _LineCount("Line Count", Range(0, 100)) = 6
        _LineStrength("Line Strength", Range(0, 1)) = 1

        [HDR]_SineGlowColor("Sine Glow Color", Color) = (0.02, 0.65, 1, 2)
        _SineGlowMin("Sine Glow Min", Range(0, 1)) = 0.75

        [HDR]_HologramTint("Hologram Tint", Color) = (0.02, 0.65, 1, 2)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Sprite Ghost 2D"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;

                float _ScrollSpeed;
                float _LineThickness;
                float _LineRotation;
                float _LineCount;
                float _LineStrength;

                float4 _SineGlowColor;
                float _SineGlowMin;

                float4 _HologramTint;
            CBUFFER_END

            float UnscaledTime;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);

                return OUT;
            }

            float2 RotateUV(float2 uv, float rotationDegrees)
            {
                float rotation = radians(rotationDegrees);
                float s = sin(rotation);
                float c = cos(rotation);

                uv -= 0.5;

                float2 rotated;
                rotated.x = uv.x * c - uv.y * s;
                rotated.y = uv.x * s + uv.y * c;

                rotated += 0.5;

                return rotated;
            }

            float StripeMask(float2 screenUV)
            {
                float2 rotatedUV = RotateUV(screenUV, _LineRotation);

                float offset = UnscaledTime * _ScrollSpeed;
                float stripeValue = frac(rotatedUV.x * _LineCount + offset);

                float width = 1.0 - _LineThickness;

                return step(width, stripeValue);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 spriteColor =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                clip(spriteColor.a - 0.01);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float stripes = StripeMask(screenUV) * _LineStrength;

                float sineGlow = sin(UnscaledTime * 4.0) * 0.5 + 0.5;
                sineGlow = max(sineGlow, _SineGlowMin);

                half4 baseColor = spriteColor * _Tint * IN.color;

                half3 glowColor =
                    (_SineGlowColor.rgb * sineGlow) +
                    (_HologramTint.rgb * stripes);

                half3 finalColor = baseColor.rgb + glowColor;

                half finalAlpha = spriteColor.a * _Tint.a;

                return half4(finalColor, finalAlpha);
            }

            ENDHLSL
        }
    }
}