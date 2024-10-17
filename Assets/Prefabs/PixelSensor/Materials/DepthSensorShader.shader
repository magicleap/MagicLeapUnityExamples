Shader "Unlit/DepthSensorShader"
{
    Properties
    {
        _MinDepth("Min Depth", Float) = 0
        _MaxDepth("Max Depth", Float) = 5
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        _MapTex("Frame Data Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MapTex;

            float4 _MainTex_ST;
            float _MinDepth;
            float _MaxDepth;

            float InverseLerp(float v, float min, float max)
            {
                return clamp((v - min) / (max - min), 0.0, 1.0);
            }

            float NormalizeDepth(float depth_meters)
            {
                return InverseLerp(depth_meters, _MinDepth, _MaxDepth);
            }

            fixed3 GetColorVisualization(float x)
            {
                return tex2D(_MapTex, fixed2(x, 0.5)).rgb;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float depth = tex2D(_MainTex, i.uv).r;
                float normalized_depth = NormalizeDepth(depth);

                fixed4 depth_color = fixed4(GetColorVisualization(normalized_depth), 1.0);
                // Values outside of range mapped to black.
                if(depth < _MinDepth || depth > _MaxDepth)
                {
                    depth_color.rgb *= 0.0;
                }
                return depth_color;
            }
            ENDCG
        }
    }
}
