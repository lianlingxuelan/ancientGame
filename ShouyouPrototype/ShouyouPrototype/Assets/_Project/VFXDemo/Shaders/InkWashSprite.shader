// ============================================================
// InkWashSprite.shader — 水墨精灵着色器
// 用途：角色立绘、技能图标、UI 元素的水墨化渲染
// 兼容：Unity 2020.3 Built-in RP（不需要 URP）
// ============================================================

Shader "VFX/InkWashSprite"
{
    Properties
    {
        _MainTex ("纹理", 2D) = "white" {}
        _InkColor ("墨色", Color) = (0.06, 0.07, 0.10, 1.0)
        _Spread ("晕染扩散", Range(0, 1)) = 0.35
        _EdgeSoftness ("边缘柔和度", Range(0, 1)) = 0.25
        _NoiseScale ("纹理噪波", Range(0.5, 10)) = 3.0
        _PaperTex ("纸纹", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _PaperTex;
            float4 _PaperTex_ST;
            fixed4 _InkColor;
            float _Spread;
            float _EdgeSoftness;
            float _NoiseScale;

            // 简易哈希
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // 2D 值噪声
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i), hash(i + float2(1, 0)), f.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x),
                    f.y);
            }

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

                // 墨色梯度：用噪声让边缘不规则
                float n = noise(i.uv * _NoiseScale + _Time.y * 0.15);
                float edge = n * _Spread;

                // 中间浓、边缘淡
                float ink = smoothstep(0.0, _EdgeSoftness + edge, tex.a);

                // 飞白：高噪声区域降低墨色密度
                float dry = noise(i.uv * _NoiseScale * 2.3 - _Time.y * 0.08) * 0.2;
                ink *= (1.0 - dry);

                // 纸纹叠加
                float4 paper = tex2D(_PaperTex, i.uv * 3.0);
                ink += paper.r * 0.06;

                // 顶点色也可影响最终 alpha
                ink *= i.color.a;

                return lerp(fixed4(0, 0, 0, 0), _InkColor, saturate(ink));
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
