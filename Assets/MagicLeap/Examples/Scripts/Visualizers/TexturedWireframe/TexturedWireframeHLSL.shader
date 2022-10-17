Shader "Unlit/TexturedWireframe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _LineWidth("Line Width", Range(0.0001, 0.05)) = 0.001
        _HighConfidenceLineColor("High Confidence Line Color", Color) = (.4, .4, .5, 1)
        _LowConfidenceLineColor("Low Confidence Line Color", Color) = (.5, 0, 0, 1)

        _HighConfidenceBackgroundColor("High Confidence Background Color", Color) = (0, 0, .04, 1)
        _LowConfidenceBackgroundColor("Low Confidence Background Color", Color) = (.04, 0, .04, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _LineWidth;
            fixed4 _HighConfidenceLineColor;
            fixed4 _LowConfidenceLineColor;

            fixed4 _HighConfidenceBackgroundColor;
            fixed4 _LowConfidenceBackgroundColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv.z = v.uv.z;
                float2 adjustedUV = v.uv.xy * (.001 / _LineWidth); // adjust for desired line width.
                o.uv.xy = TRANSFORM_TEX(adjustedUV.xy, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 backgroundColor = lerp(_LowConfidenceBackgroundColor, _HighConfidenceBackgroundColor, i.uv.z);
                fixed4 lineColor = tex2D(_MainTex, i.uv.xy) * lerp(_LowConfidenceLineColor, _HighConfidenceLineColor, i.uv.z);
                return backgroundColor * (1 - lineColor.a) + lineColor;
            }
            ENDCG
        }
    }
}
