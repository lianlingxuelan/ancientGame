// ============================================================
// InkWashVFXDemo.cs — 水墨特效演示
// ============================================================
// 用法：
//   1. 创建空 GameObject，挂上这个脚本
//   2. 点击 Play
//   3. 操作：
//      鼠标左键按住拖动 → 剑气拖尾（Trail Renderer）
//      鼠标右键点击       → 墨滴粒子爆发
//      空格键              → 全屏墨溅大爆发
//      1/2/3              → 切换墨色（青墨/焦墨/朱砂）
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InkWashVFXDemo : MonoBehaviour
{
    [Header("材质")]
    public Material inkWashParticleMaterial;
    public Material inkWashSpriteMaterial;

    [Header("墨色预设")]
    public Color[] inkColors = new Color[]
    {
        new Color(0.30f, 0.48f, 0.52f), // 青墨（偏蓝绿）
        new Color(0.08f, 0.07f, 0.10f), // 焦墨（近黑）
        new Color(0.55f, 0.20f, 0.18f), // 朱砂（暗红）
    };

    private int _currentColorIndex;
    private GameObject _swordTrail;
    private TrailRenderer _trailRenderer;

    // ============================================================
    // 启动
    // ============================================================
    void Start()
    {
        // 自动创建材质（如果没有手动拖入）
        if (inkWashParticleMaterial == null)
        {
            var shader = Shader.Find("VFX/InkWashParticle");
            if (shader != null)
                inkWashParticleMaterial = new Material(shader);
        }
        if (inkWashSpriteMaterial == null)
        {
            var shader = Shader.Find("VFX/InkWashSprite");
            if (shader != null)
                inkWashSpriteMaterial = new Material(shader);
        }

        CreateSwordTrail();
        ShowInstructions();
    }

    void CreateSwordTrail()
    {
        // 剑气拖尾：一个隐形的 GameObject + Trail Renderer
        _swordTrail = new GameObject("SwordTrail");
        _swordTrail.transform.SetParent(transform);

        _trailRenderer = _swordTrail.AddComponent<TrailRenderer>();
        _trailRenderer.time = 0.28f;
        _trailRenderer.startWidth = 0.20f;
        _trailRenderer.endWidth = 0.02f;
        _trailRenderer.minVertexDistance = 0.05f;
        _trailRenderer.autodestruct = false;

        // 颜色渐变：浓→淡（水墨收笔）
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(inkColors[0], 0f),
                new GradientColorKey(inkColors[0] * 0.6f, 0.5f),
                new GradientColorKey(inkColors[0] * 0.2f, 1f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.4f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        _trailRenderer.colorGradient = gradient;

        // 使用水墨精灵 Shader 做材质
        if (inkWashSpriteMaterial != null)
            _trailRenderer.material = inkWashSpriteMaterial;
        else
            _trailRenderer.material = new Material(Shader.Find("Sprites/Default"));

        _trailRenderer.sortingOrder = 10;
        _trailRenderer.emitting = false;
    }

    void ShowInstructions()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("  水墨 VFX 演示 - 操作说明");
        Debug.Log("  左键拖动 → 剑气拖尾");
        Debug.Log("  右键点击 → 墨滴粒子");
        Debug.Log("  空格     → 全屏大爆发");
        Debug.Log("  1/2/3    → 切换墨色");
        Debug.Log("═══════════════════════════════════════");
    }

    // ============================================================
    // 每帧
    // ============================================================
    void Update()
    {
        var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // 左键 → 剑气拖尾
        if (Input.GetMouseButton(0))
        {
            _swordTrail.transform.position = mouseWorld;
            if (!_trailRenderer.emitting)
                _trailRenderer.emitting = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            _trailRenderer.emitting = false;
            // 松手时尾部自动消散（Trail Renderer 的 time 参数控制）
        }

        // 右键 → 墨滴粒子
        if (Input.GetMouseButtonDown(1))
        {
            BurstInkParticles(mouseWorld, 40, 1.5f);
        }

        // 空格 → 大爆发
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BurstInkParticles(mouseWorld, 120, 3.0f);
            StartCoroutine(ShockwaveEffect(mouseWorld));
        }

        // 1/2/3 切换颜色
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchInkColor(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchInkColor(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchInkColor(2);
    }

    // ============================================================
    // 墨滴粒子爆发
    // ============================================================
    void BurstInkParticles(Vector2 position, int count, float radius)
    {
        var go = new GameObject("InkBurst_" + Time.frameCount);
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startColor = inkColors[_currentColorIndex];
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, count)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        // 速度随生命周期变化
        var velOverLifetime = ps.velocityOverLifetime;
        velOverLifetime.enabled = true;
        velOverLifetime.space = ParticleSystemSimulationSpace.World;
        velOverLifetime.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
        velOverLifetime.y = new ParticleSystem.MinMaxCurve(-1f, 1f);

        // 颜色随生命周期 → 墨色从浓到淡
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var colGrad = new Gradient();
        colGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(inkColors[_currentColorIndex], 0f),
                new GradientColorKey(inkColors[_currentColorIndex] * 0.4f, 0.7f),
                new GradientColorKey(inkColors[_currentColorIndex] * 0.1f, 1f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.3f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        colorOverLifetime.color = colGrad;

        // 大小随生命周期 → 墨点扩散后消散
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.2f, 1.3f),
            new Keyframe(1f, 0.1f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 重力 → 墨滴微沉
        var gravity = ps.main;
        gravity.gravityModifier = 0.3f;

        // 使用水墨粒子材质
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = inkWashParticleMaterial != null
                ? inkWashParticleMaterial
                : new Material(Shader.Find("Particles/Additive"));
            renderer.sortingOrder = 20;
        }

        // 自动销毁
        Destroy(go, 2f);
    }

    // ============================================================
    // 冲击波效果（法阵扩散）
    // ============================================================
    IEnumerator ShockwaveEffect(Vector2 position)
    {
        // 创建一个扩散的圆环（缩放 Sprite）
        var ring = new GameObject("Shockwave");
        ring.transform.position = position;

        var sr = ring.AddComponent<SpriteRenderer>();
        // 生成一张圆形贴图
        var tex = CreateCircleTexture(128, inkColors[_currentColorIndex]);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), Vector2.one * 0.5f);
        sr.sortingOrder = 15;

        if (inkWashSpriteMaterial != null)
            sr.material = inkWashSpriteMaterial;

        float t = 0;
        float duration = 0.6f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float scale = Mathf.Lerp(0.3f, 3f, p);
            ring.transform.localScale = Vector3.one * scale;

            var c = sr.color;
            c.a = 1f - p;
            sr.color = c;

            yield return null;
        }

        Destroy(ring);
    }

    // ============================================================
    // 工具函数
    // ============================================================
    void SwitchInkColor(int index)
    {
        _currentColorIndex = Mathf.Clamp(index, 0, inkColors.Length - 1);
        Debug.Log("墨色切换：" + new[] { "青墨", "焦墨", "朱砂" }[_currentColorIndex]);

        // 更新拖尾颜色
        if (_trailRenderer != null)
        {
            var col = inkColors[_currentColorIndex];
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(col, 0f),
                    new GradientColorKey(col * 0.6f, 0.5f),
                    new GradientColorKey(col * 0.2f, 1f),
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.5f, 0.4f),
                    new GradientAlphaKey(0f, 1f),
                }
            );
            _trailRenderer.colorGradient = gradient;
        }
    }

    Texture2D CreateCircleTexture(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 中间浓、边缘淡 → 水墨感
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = alpha * alpha; // 二次衰减
                // 边缘微微抖动模拟毛笔笔触
                float edge = Mathf.Abs(dist - radius * 0.8f);
                if (edge < 3f) alpha *= 0.7f;

                pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
