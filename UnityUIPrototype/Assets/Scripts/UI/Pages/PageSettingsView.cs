using UnityEngine;

namespace Shouyou.UI.Pages
{
    public sealed class PageSettingsView : UIPageBase
    {
        [Header("Settings Page")]
        [SerializeField] private bool enableMusic = true;
        [SerializeField] private bool enableSfx = true;
    }
}

