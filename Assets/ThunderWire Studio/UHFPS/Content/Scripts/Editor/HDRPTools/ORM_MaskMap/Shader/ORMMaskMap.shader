Shader "Custom/ORMMaskMap"
{
    Properties
    {
        _ORM("Texture", 2D) = "white" {}
        _R("_R", Float) = 1
        _G("_G", Float) = 1
        _B("_B", Float) = 1
        _A("_A", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _ORM;
            float _R, _G, _B, _A;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 applyColorChannel(float value, float channel)
            {
                if (value == 1) return 0; // black
                else if (value == 2) return 1; // white
                else return channel; // default channel color
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ORM (R = Occlusion, G = Roughness, B = Metallic)
                // MaskMap (R = Metallic, G = Occlusion, B = Black, A = Smoothness)
                fixed4 col = tex2D(_ORM, i.uv);
                fixed4 newCol = (0, 0, 0, 0);

                newCol.r = applyColorChannel(_R, col.b);
                newCol.g = applyColorChannel(_G, col.r);
                newCol.b = applyColorChannel(_B, 0);
                newCol.a = applyColorChannel(_A, 1 - col.g);

                return newCol;
            }
            ENDCG
        }
    }
}