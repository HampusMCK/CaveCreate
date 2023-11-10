Shader "Minecraft/Blocks"
{
    Properties
    {
        _MainTex ("Block Texure Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
        LOD 100
        Lighting off

        Pass 
        {
            CGPROGRAM
                #pragma vertex vertFunction
                #pragma fragment fragFunction
                #pragma target 2.0

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

                sampler2D _MainTex;
                float GlobalLightLevel;

                v2f vertFunction (appdata v)
                {
                    v2f o;

                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;

                    return o;
                }

                fixed4 fragFunction (v2f i) : SV_Target
                {
                    fixed4 col = tex2D (_MainTex, i.uv);
                    clip(col.a - 0.35);
                    col = lerp(col, float4(0, 0, 0, 1), GlobalLightLevel);

                    return col;
                }

            ENDCG
        }
    }
}