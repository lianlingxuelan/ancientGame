using UnityEngine;

namespace Shouyou.UI.Theme
{
    // 单套 UI 主题配置。
    // 它把背景图、按钮图、文字颜色集中放在一起，避免每个页面到处写死颜色。
    // 设计思路：页面布局尽量共用，现实/梦域只更换皮肤资源和颜色。
    [CreateAssetMenu(menuName = "Shouyou/UI/Theme Config", fileName = "UIThemeConfig")]
    public sealed class UIThemeConfig : ScriptableObject
    {
        [Header("主题基础信息")]
        [Tooltip("当前主题类型：现实或梦域")]
        public UIThemeType themeType = UIThemeType.Reality;

        [Tooltip("给策划/美术看的名字，例如：现实清雅主题、梦域星蝶主题")]
        public string displayName = "现实清雅主题";

        [Header("背景与框体")]
        [Tooltip("页面主背景图，例如庭院背景或梦域通用背景")]
        public Sprite pageBackground;

        [Tooltip("主内容面板边框/底图")]
        public Sprite mainPanelSprite;

        [Tooltip("普通卡片边框/底图，例如关卡卡、任务卡")]
        public Sprite cardSprite;

        [Header("按钮")]
        [Tooltip("普通按钮底图")]
        public Sprite normalButtonSprite;

        [Tooltip("选中按钮底图，例如左侧栏目选中态")]
        public Sprite selectedButtonSprite;

        [Header("颜色")]
        [Tooltip("大标题颜色")]
        public Color titleColor = new Color32(122, 79, 40, 255);

        [Tooltip("普通正文颜色")]
        public Color bodyTextColor = new Color32(70, 48, 36, 255);

        [Tooltip("按钮文字颜色")]
        public Color buttonTextColor = new Color32(201, 154, 90, 255);

        [Tooltip("普通面板颜色，当没有图片时作为兜底")]
        public Color panelFallbackColor = new Color32(255, 248, 236, 210);

        [Tooltip("选中按钮颜色，当没有图片时作为兜底")]
        public Color selectedFallbackColor = new Color32(238, 190, 125, 220);
    }
}
