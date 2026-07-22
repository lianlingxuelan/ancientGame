using UnityEngine;
using UnityEngine.UI;

namespace Shouyou.UI.Theme
{
    // 单个 UI 节点的主题标记。
    // 把这个脚本挂到 Image 或 Text 所在对象上，UIThemeApplier 就能统一给它换皮肤。
    public sealed class UIThemeElement : MonoBehaviour
    {
        // 元素类型决定它会从 UIThemeConfig 里取哪一种资源。
        public UIThemeElementRole role = UIThemeElementRole.BodyText;

        [Tooltip("是否允许主题系统替换 Image 的 Sprite")]
        public bool applySprite = true;

        [Tooltip("是否允许主题系统替换颜色")]
        public bool applyColor = true;

        // 由主题管理器调用：根据当前主题刷新这个节点。
        public void Apply(UIThemeConfig config)
        {
            if (config == null)
            {
                return;
            }

            Image image = GetComponent<Image>();
            if (image != null)
            {
                ApplyImage(image, config);
            }

            Text text = GetComponent<Text>();
            if (text != null)
            {
                ApplyText(text, config);
            }
        }

        private void ApplyImage(Image image, UIThemeConfig config)
        {
            if (applySprite)
            {
                Sprite nextSprite = GetSprite(config);
                if (nextSprite != null)
                {
                    image.sprite = nextSprite;
                    // 背景图应该整张铺满；按钮、面板、卡片才适合九宫格拉伸。
                    image.type = role == UIThemeElementRole.PageBackground ? Image.Type.Simple : Image.Type.Sliced;
                }
            }

            if (applyColor)
            {
                image.color = GetColor(config);
            }
        }

        private void ApplyText(Text text, UIThemeConfig config)
        {
            if (!applyColor)
            {
                return;
            }

            text.color = GetColor(config);
        }

        private Sprite GetSprite(UIThemeConfig config)
        {
            switch (role)
            {
                case UIThemeElementRole.PageBackground:
                    return config.pageBackground;
                case UIThemeElementRole.MainPanel:
                    return config.mainPanelSprite;
                case UIThemeElementRole.Card:
                    return config.cardSprite;
                case UIThemeElementRole.NormalButton:
                    return config.normalButtonSprite;
                case UIThemeElementRole.SelectedButton:
                    return config.selectedButtonSprite;
                default:
                    return null;
            }
        }

        private Color GetColor(UIThemeConfig config)
        {
            switch (role)
            {
                case UIThemeElementRole.TitleText:
                    return config.titleColor;
                case UIThemeElementRole.BodyText:
                    return config.bodyTextColor;
                case UIThemeElementRole.NormalButton:
                    return config.panelFallbackColor;
                case UIThemeElementRole.SelectedButton:
                    return config.selectedFallbackColor;
                case UIThemeElementRole.ButtonText:
                    return config.buttonTextColor;
                case UIThemeElementRole.MainPanel:
                case UIThemeElementRole.Card:
                    return config.panelFallbackColor;
                default:
                    return Color.white;
            }
        }
    }

    // 主题元素角色。
    // 这里拆细一点，是为了以后不用写很多 if 判断，也方便你在 Inspector 里看懂每个对象属于什么类型。
    public enum UIThemeElementRole
    {
        PageBackground,
        MainPanel,
        Card,
        NormalButton,
        SelectedButton,
        TitleText,
        BodyText,
        ButtonText
    }
}
