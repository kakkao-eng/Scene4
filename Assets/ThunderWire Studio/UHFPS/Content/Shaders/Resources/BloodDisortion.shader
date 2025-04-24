Shader "UHFPS/BloodDisortion"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    float4 _BlendColor;
    float4 _OverlayColor;

    float _BloodAmount;
    float _BlendAmount;
    float _EdgeSharpness;
    float _Distortion;

    TEXTURE2D_X(_InputTexture);

    TEXTURE2D(_BlendTex);
    SAMPLER(sampler_BlendTex);

    TEXTURE2D(_BumpMap);
    SAMPLER(sampler_BumpMap);

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        half4 blendColor = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, input.texcoord);
        half4 bumpColor = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.texcoord);

        blendColor.a = blendColor.a + (_BlendAmount * 2 - 1);
        blendColor.a = saturate(blendColor.a * _EdgeSharpness - (_EdgeSharpness - 1) * 0.5);

        half2 bump = UnpackNormal(bumpColor).rg;
        float2 distortedUV = input.texcoord.xy + bump * blendColor.a * _Distortion;
        half4 mainColor = LOAD_TEXTURE2D_X(_InputTexture, distortedUV * _ScreenSize.xy);
        half4 overlayColor = blendColor;

        overlayColor.rgb = mainColor.rgb * (blendColor.rgb + 0.5) * 1.2;
        blendColor = lerp(blendColor, overlayColor, 0.3);
        mainColor.rgb *= 1 - blendColor.a * 0.5;

        half4 overlay = lerp(float4(1, 1, 1, 1), _OverlayColor, _BloodAmount);
        half4 color = lerp(mainColor, blendColor * _BlendColor, blendColor.a) * overlay;

        half4 def = LOAD_TEXTURE2D_X(_InputTexture, input.texcoord * _ScreenSize.xy);
        return lerp(def, color, _BloodAmount);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Melt"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
