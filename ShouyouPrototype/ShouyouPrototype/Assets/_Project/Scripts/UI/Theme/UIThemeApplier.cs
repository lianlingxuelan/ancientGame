using UnityEngine;

namespace Shouyou.UI.Theme
{
    // UI 主题应用器。
    // 它通常挂在 Canvas 上，负责把 Reality / Dream 两套主题应用到当前页面。
    public sealed class UIThemeApplier : MonoBehaviour
    {
        [Header("主题配置")]
        [Tooltip("现实主题：庭院、雅集、现世凡尘")]
        [SerializeField] private UIThemeConfig realityTheme;

        [Tooltip("梦域主题：星蝶、紫蓝、神识梦境")]
        [SerializeField] private UIThemeConfig dreamTheme;

        [Tooltip("当前使用的主题")]
        [SerializeField] private UIThemeType currentTheme = UIThemeType.Reality;

        // 当前主题配置，给其它 UI 脚本读取。
        public UIThemeConfig CurrentConfig
        {
            get
            {
                return currentTheme == UIThemeType.Dream ? dreamTheme : realityTheme;
            }
        }

        private void Awake()
        {
            // 进入游戏时先应用一次，避免页面初始颜色不统一。
            ApplyCurrentTheme();
        }

        // 切换到现实主题。
        public void UseRealityTheme()
        {
            currentTheme = UIThemeType.Reality;
            ApplyCurrentTheme();
        }

        // 切换到梦域主题。
        public void UseDreamTheme()
        {
            currentTheme = UIThemeType.Dream;
            ApplyCurrentTheme();
        }

        // 在现实和梦域之间来回切换。
        public void ToggleTheme()
        {
            currentTheme = currentTheme == UIThemeType.Reality ? UIThemeType.Dream : UIThemeType.Reality;
            ApplyCurrentTheme();
        }

        // 应用当前主题到所有带 UIThemeElement 标记的对象。
        public void ApplyCurrentTheme()
        {
            UIThemeConfig config = CurrentConfig;
            if (config == null)
            {
                Debug.LogWarning("UIThemeApplier：当前主题配置为空，暂时无法应用主题。");
                return;
            }

            UIThemeElement[] elements = GetComponentsInChildren<UIThemeElement>(true);
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].Apply(config);
            }
        }
    }
}
