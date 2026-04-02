Shader "Custom/AtmosphereFresnel" {
    Properties {
        _Color ("大气颜色 (Atmosphere Color)", Color) = (0.35, 0.65, 1.0, 1.0)
        _FresnelPower ("边缘厚度 (Fresnel Power)", Range(0.5, 10.0)) = 4.0
        _GlowIntensity ("发光强度 (Glow Intensity)", Range(0.1, 5.0)) = 1.5
    }
    SubShader {
        // 设置为透明队列，不遮挡里面的地球
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            float4 _Color;
            float _FresnelPower;
            float _GlowIntensity;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                
                // 计算菲涅耳核心：法线与视线的点乘
                float ndotv = max(0, dot(normal, viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower);
                
                // 颜色和透明度都会向边缘富集
                float3 finalColor = _Color.rgb * _GlowIntensity;
                return float4(finalColor, fresnel * _Color.a);
            }
            ENDCG
        }
    }
}