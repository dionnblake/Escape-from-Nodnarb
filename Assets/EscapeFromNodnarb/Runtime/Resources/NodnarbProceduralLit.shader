Shader "EscapeFromNodnarb/ProceduralLit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half light : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            fixed4 _Color;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float3 worldNormal = normalize(UnityObjectToWorldNormal(input.normal));
                float3 lightDirection = normalize(float3(-0.35, 0.78, -0.52));
                output.light = 0.52h + 0.48h * saturate(dot(worldNormal, lightDirection));
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = fixed4(_Color.rgb * input.light, _Color.a);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
