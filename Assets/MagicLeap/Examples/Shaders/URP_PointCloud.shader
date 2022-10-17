// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

Shader "Magic Leap/URP/Point Cloud"
{
    Properties
    {
        PointSize("Point Size", Float) = 5
    }

    Category
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        SubShader
        {
            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct appdata
                {
                    float4 vertex : POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : POSITION;
                    float4 pointColor : COLOR;
                    float pointSize : PSIZE;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                float PointSize;

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.vertex = TransformObjectToHClip(v.vertex.xyz);
                    o.pointSize = PointSize;

                    const float4 colors[11] = {
                        float4(1.0, 1.0, 1.0, 1.0),  // White
                        float4(1.0, 0.0, 0.0, 1.0),  // Red
                        float4(0.0, 1.0, 0.0, 1.0),  // Green
                        float4(0.0, 0.0, 1.0, 1.0),  // Blue
                        float4(1.0, 1.0, 0.0, 1.0),  // Yellow
                        float4(0.0, 1.0, 1.0, 1.0),  // Cyan/Aqua
                        float4(1.0, 0.0, 1.0, 1.0),  // Magenta
                        float4(0.5, 0.0, 0.0, 1.0),  // Maroon
                        float4(0.0, 0.5, 0.5, 1.0),  // Teal
                        float4(1.0, 0.65, 0.0, 1.0), // Orange
                        float4(1.0, 1.0, 1.0, 1.0)   // White
                    };

                    float cameraToVertexDistance = distance(_WorldSpaceCameraPos, v.vertex.xyz);
                    int index = clamp(floor(cameraToVertexDistance), 0, 10);

                    o.pointColor = colors[index];

                    return o;
                }

                float4 frag(v2f i) : SV_Target
                {
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(i);
                    return i.pointColor;
                }
                ENDHLSL
            }
        }
    }
}
