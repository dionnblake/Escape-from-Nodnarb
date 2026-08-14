Shader "EscapeFromNodnarb/ProceduralLit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base Map", 2D) = "white" {}
        _EmissionMap ("Emission Mask", 2D) = "black" {}
        _EmissionColor ("Emission", Color) = (0, 0, 0, 0)
        _EmissionStrength ("Emission Strength", Range(0, 4)) = 1
        _ShadowColor ("Shadow Tint", Color) = (0.32, 0.24, 0.22, 1)
        _DirectionalStrength ("Directional Shading", Range(0, 1)) = 0.82
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.38
        _VertexColorStrength ("Vertex Color Strength", Range(0, 1)) = 0
        _RimColor ("Rim Color", Color) = (0.12, 0.32, 0.20, 1)
        _RimPower ("Rim Power", Range(0.5, 6)) = 2.4
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.12
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
                fixed4 vertexColor : COLOR;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _EmissionMap;
            fixed4 _EmissionColor;
            half _EmissionStrength;
            fixed4 _ShadowColor;
            half _DirectionalStrength;
            half _AmbientStrength;
            half _VertexColorStrength;
            fixed4 _RimColor;
            half _RimPower;
            half _RimStrength;
            fixed4 _SpecularColor;
            half _SpecularStrength;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertexColor = v.color;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_LIGHT_ATTENUATION(attenuation, input, input.worldPosition);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - input.worldPosition);
                half direct = saturate(dot(input.worldNormal, lightDirection));
                fixed4 baseColor = tex2D(_MainTex, input.uv) * _Color;
                baseColor.rgb *= lerp(fixed3(1, 1, 1), input.vertexColor.rgb, _VertexColorStrength);
                half directional = saturate(direct * attenuation);
                half lighting = saturate(_AmbientStrength + _DirectionalStrength * (0.24h + 0.76h * directional));
                fixed3 shadowedBase = lerp(baseColor.rgb * _ShadowColor.rgb, baseColor.rgb, directional);
                half rim = pow(1.0h - saturate(dot(input.worldNormal, viewDirection)), _RimPower) * _RimStrength;
                half specular = pow(saturate(dot(reflect(-lightDirection, input.worldNormal), viewDirection)), 16.0h)
                    * _SpecularStrength * attenuation;
                fixed3 emission = tex2D(_EmissionMap, input.uv).rgb * _EmissionColor.rgb * _EmissionStrength;
                fixed3 surface = shadowedBase * lighting;
                surface += _RimColor.rgb * rim;
                surface += _SpecularColor.rgb * specular;
                fixed4 color = fixed4(surface + emission, baseColor.a);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdataShadow
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2fShadow
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                V2F_SHADOW_CASTER;
            };

            v2fShadow vertShadow(appdataShadow v)
            {
                v2fShadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2fShadow input) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(input)
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
