Shader "Overclocked/CPUStationOutlineComposite"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "CPUStationOutlineComposite"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_CPUStationOutlineMaskTexture);
            SAMPLER(sampler_CPUStationOutlineMaskTexture);
            float _CPUStationOutlineThickness;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
                half4 sourceColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 centerMask = SAMPLE_TEXTURE2D_X(_CPUStationOutlineMaskTexture, sampler_CPUStationOutlineMaskTexture, uv);

                if (centerMask.a > 0.001h)
                {
                    return sourceColor;
                }

                float thickness = max(1.0, _CPUStationOutlineThickness);
                float2 texelOffset = _BlitTexture_TexelSize.xy * thickness;

                float2 offsets[8] =
                {
                    float2(-1, 0),
                    float2(1, 0),
                    float2(0, -1),
                    float2(0, 1),
                    float2(-1, -1),
                    float2(-1, 1),
                    float2(1, -1),
                    float2(1, 1)
                };

                half maxAlpha = 0.0h;
                half3 outlineColor = half3(0, 0, 0);

                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    half4 neighborMask = SAMPLE_TEXTURE2D_X(
                        _CPUStationOutlineMaskTexture,
                        sampler_CPUStationOutlineMaskTexture,
                        uv + offsets[i] * texelOffset
                    );

                    if (neighborMask.a > maxAlpha)
                    {
                        maxAlpha = neighborMask.a;
                        outlineColor = neighborMask.rgb;
                    }
                }

                half outlineAlpha = saturate(maxAlpha);
                half3 finalColor = lerp(sourceColor.rgb, outlineColor, outlineAlpha);
                return half4(finalColor, sourceColor.a);
            }
            ENDHLSL
        }
    }
}
