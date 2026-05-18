Shader "WorldspaceGrid"
{
    Properties 
    {
        _Mask ("Mask", 2D) = "white" {}
        _GridThickness ("Grid Thickness", Float) = 0.01
        _GridSpacing ("Grid Spacing", Float) = 10.0
        _GridColour ("Grid Colour", Color) = (0.5, 1.0, 1.0, 1.0)
        _BaseColour ("Base Colour", Color) = (0.0, 0.0, 0.0, 0.0)
    }
     
    SubShader 
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
     
        Pass 
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
     
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            sampler2D _Mask;
            float4 _Mask_ST;

            float _GridThickness;
            float _GridSpacing;
            float4 _GridColour;
            float4 _BaseColour;
     
            struct vertexInput 
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
 
            struct vertexOutput 
            {
                float4 pos : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };
     
            vertexOutput vert(vertexInput input) 
            {
                vertexOutput output;

                output.pos = UnityObjectToClipPos(input.vertex);

                output.texcoord = input.texcoord * _Mask_ST.xy + _Mask_ST.zw;

                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;

                return output;
            }
 
            float4 frag(vertexOutput input) : SV_Target
            {
                float maskAlpha = tex2D(_Mask, input.texcoord).a;

                float offset = _GridThickness * 0.5;

                float2 wp = input.worldPos.xz + offset;

                float lineX = step(frac(wp.x / _GridSpacing), _GridThickness);
                float lineZ = step(frac(wp.y / _GridSpacing), _GridThickness);

                float lineMask = saturate(lineX + lineZ);

                float4 col = lerp(_BaseColour, _GridColour, lineMask);

                col.a *= maskAlpha;

                return col;
            }

            ENDCG
        }
    }
}