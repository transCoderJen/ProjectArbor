Shader "CustomRenderTexture/CloudShadow"
{
    Properties
    {
        _Speed ("Speed", Vector) = (0.1,0,0.1,0)
        _MainTex("InputTex", 2D) = "white" {}
        _MainTex2("InputTex", 2D) = "white" {}
        _Darkness ("Darkness", Range(0, 2)) = 1
        [Toggle] _Invert ("Invert", Float) = 0
    }

    SubShader
    {
        Blend One Zero
        Lighting Off

        Pass
        {
            Name "New Custom Render Texture"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4      _Speed;
            float       _Darkness;
            float       _Invert;

            sampler2D   _MainTex;
            float4      _MainTex_A;
            sampler2D   _MainTex2;
            float4      _MainTex2_A;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float4 cloud1 = tex2D(_MainTex, IN.localTexcoord.xy + frac(_Time * _Speed.xy));
                float4 cloud2 = tex2D(_MainTex2, IN.localTexcoord.xy + frac(_Time * _Speed.zw));

                float4 result = max(0.1f, cloud1 * cloud2);

                // Darkness control
                result = 1.0 - ((1.0 - result) * _Darkness);

                // Invert toggle
                result = lerp(result, 1.0 - result, _Invert);

                return saturate(result);
            }
            ENDCG
        }
    }
}