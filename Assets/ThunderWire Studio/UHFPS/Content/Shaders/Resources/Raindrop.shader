Shader "UHFPS/Raindrop"
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

    TEXTURE2D_X(_InputTexture);
    TEXTURE2D(_DropletsMask);
    SAMPLER(sampler_DropletsMask);

    float2 _Tiling;
    float _Raining;
    float _Distortion;
    float _DistortionScale;
    float _GlobalRotation;
    float _DropletsGravity;
    float _DropletsSpeed;
    float _DropletsStrength;

    float Remap(float In, float2 InMinMax, float2 OutMinMax)
    {
        return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
    }

    float2 Rotate(float2 UV, float2 Center, float Rotation)
    {
        UV -= Center;
        float s = sin(Rotation);
        float c = cos(Rotation);
        float2x2 rMatrix = float2x2(c, -s, s, c);
        rMatrix *= 0.5;
        rMatrix += 0.5;
        rMatrix = rMatrix * 2 - 1;
        UV.xy = mul(UV.xy, rMatrix);
        UV += Center;
        return UV;
    }

    float4 ComputeRaindrops(float4 raindropMask)
    {
        float droplets = Remap(raindropMask.b, float2(0, 1), float2(-1, 1));
        droplets += _Time.y * _DropletsSpeed;
        droplets = frac(droplets);
        droplets = raindropMask.a - droplets;

        float2 dropletStrength = float2(1.0 - _DropletsStrength, 1.0);
        droplets = Remap(droplets, dropletStrength, float2(0, 1));
        droplets = ceil(droplets);
        droplets = saturate(droplets);

        float4 splitMask = raindropMask * 2.0 - 1.0;
        return splitMask * droplets;
    }

    half4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord * _Tiling;
        float2 raindropGravity = float2(0.0, _DropletsGravity);
        float2 raindropPan = _Time.y * raindropGravity + uv;
        float2 rotation = Rotate(raindropPan, float2(0.5, 0.5), radians(_GlobalRotation));

        float4 raindropMask = SAMPLE_TEXTURE2D(_DropletsMask, sampler_DropletsMask, rotation);
        float4 raindrops = ComputeRaindrops(raindropMask);

        float4 rain = raindrops * _Raining;
        rain *= _Distortion * _DistortionScale;

        float2 distortedCoords = input.texcoord * (_ScreenSize.xy + rain.xy);
        float4 color = LOAD_TEXTURE2D_X(_InputTexture, distortedCoords);
        return color;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Raindrop"

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