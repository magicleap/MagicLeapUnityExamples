// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

Shader "Magic Leap/URP/Drawing Canvas"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "black" {}
        _Pen("Pen", 2D) = "black" {}
    }

    Category
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Blend One One
        ZWrite Off

        SubShader
        {
            Pass
            {
                HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment frag
    
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv0 : TEXCOORD0;

                    UNITY_VERTEX_OUTPUT_STEREO
                };

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                sampler2D _Pen;
                sampler2D _MainTex;
                float4 _MainTex_ST;

                v2f vert(appdata v)
                {
                    v2f o;

                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.pos = TransformObjectToHClip(v.vertex.xyz);
                    o.uv0 = TRANSFORM_TEX(v.texcoord, _MainTex);

                    return o;
                }

                float4 frag(v2f o) : COLOR
                {
                    float4 col = tex2D(_Pen, o.uv0);

                    if (col.a < 0.1f) discard;
                    return col;
                }

                ENDHLSL
            }
        }
    }
}
