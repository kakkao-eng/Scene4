Shader "UHFPS/EyeBlink"
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

    float _VignetteOuterRing;
    float _VignetteInnerRing;
    float _VignetteAspectRatio;
    float _Blink;

    TEXTURE2D_X(_InputTexture);

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float4 color = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

        float aspectRatio = _VignetteAspectRatio + 1.0 / (1.0 - _Blink) - 1.0;
        float2 vd = float2(input.texcoord.x - 0.5, (input.texcoord.y - 0.5) * aspectRatio);

        float vb = 1.0 - _Blink * 2.0;
        vb = max(0.0, vb);

        float outerRing = 1.0 - _VignetteOuterRing;
        float innerRing = 1.0 - _VignetteInnerRing;

        if (innerRing >= outerRing) {
            innerRing = outerRing - 0.0001f;
        }

        float vignetteEffect = saturate((dot(vd, vd) - outerRing) / (innerRing - outerRing));
        vignetteEffect = saturate(vignetteEffect - (_Blink * 0.5));

        float3 vcolor = lerp(color.rgb * vb, color.rgb, vignetteEffect);
        return float4(vcolor, color.a);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "EyeBlink"

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