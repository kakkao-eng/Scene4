Shader "UHFPS/FearTentacles"
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

    float _EffectTime;
    float _EffectFade;
    float _TentaclesPosition;
    float _LayerPosition;
    float _VignetteStrength;
    int _NumOfTentacles;
    int _ShowLayer;

    TEXTURE2D_X(_InputTexture);

    float rand(float2 n) 
    {
        return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
    }

    float tent(float2 uv, float id) 
    {
        float iTime = _EffectTime;
        float tentPos = clamp(_TentaclesPosition, -0.2, 0.2);
        float offset = tentPos * sign(uv.y - 0.5);
        float rv = rand(float2(id + sign(uv.y - 0.5), id));
        float2 st = uv;

        uv.y += offset;
        uv.x += rv * 0.1;
        uv.y += 0.05;

        float r = min(0.45 + _EffectFade * 0.02, 0.48) + (rv - 0.5) * 0.0;
        r += abs(uv.y - 0.5) * (0.1 + (rv - 0.5) * 0.05);

        uv.x += sin(uv.y * (3.0 + rv) + iTime * rv * 2.0 + rv * 3.0 + id * 20.0) * (0.1 + (sin(rv) * 0.1) * 0.1);
        uv.x += uv.y * (rv - 0.5) * 0.4;
        uv.y += 0.05;
        uv.y += sin(uv.x * 20.0) * 0.05;
        uv.y += sin(st.x * rv * rv * 120.0 + iTime + rv + id) * 0.05 * rv;

        float lay = 1.0;
        if (_ShowLayer) 
        {
            uv.y -= offset;
            uv.y += _LayerPosition * sign(uv.y - 0.5);
            lay = lerp(1., 0.82, smoothstep(0.57, 0.6, abs(uv.y - 0.5)));
        }

        return (1.0 - smoothstep(r, r - 0.05, abs(uv.x - 0.5) + 0.5)) * lay;
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float4 color = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

        float2 uv = input.texcoord;
        float s = 1.0;

        for (int i = 0; i < 100; i++)
        {
            if (i >= _NumOfTentacles) break;
            float2 randValue = float2(rand(float2(float(i), 0.0)) - 0.5, 0.01);
            s *= tent(uv + randValue, sin(float(i) * 200.0) * 2003.0);
        }

        uv *= 1.0 - uv.yx;
        float vig = uv.x * uv.y * 5.0;
        vig = pow(vig, _EffectFade * _VignetteStrength);

        s = lerp(1.0, s, _EffectFade);
        s *= vig;

        return float4(color.rgb * s, color.a);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Tentacles"

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