// ============================================================
// InkWashParticle.shader — 水墨粒子着色器
// 用途：墨滴飞溅、灵光粒子、命中火花
// 效果：Additive 叠加 + 墨色渐变 + 边缘晕开
// 兼容：Unity 2020.3 Built-in RP
// ============================================================

Shader "VFX/InkWashParticle"
{
    Properties
    {
        _MainTex ("粒子贴图", 2D) = "white" {}
        _TintColor ("色调", Color) = (0.5, 0.6, 0.7, 1.0)
        _Brightness ("亮度", Range(0, 3)) = 1.2
        _Softness ("柔边", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        // Additive 混合 → 发光感，但压低亮度保持水墨的"沉"
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            float _Brightness;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // 柔边：让粒子不是硬边圆点
                float soft = smoothstep(0.0, _Softness, tex.a);

                // 色调混合
                fixed4 col = _TintColor * _Brightness;
                col.rgb *= i.color.rgb;  // 粒子系统颜色叠加
                col.a = soft * i.color.a * tex.a;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Particles/Additive"
}
