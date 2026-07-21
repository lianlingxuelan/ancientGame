using System.Collections.Generic;
using UnityEngine;

namespace Shouyou.UI
{
    public sealed class UIPageRegistry : MonoBehaviour
    {
        [SerializeField] private List<UIPageBase> pages = new();

        private readonly Dictionary<UIPageId, UIPageBase> pageMap = new();
        private UIPageBase currentPage;

        private void Awake()
        {
            pageMap.Clear();

            foreach (var page in pages)
            {
                if (page == null)
                {
                    continue;
                }

                if (!pageMap.ContainsKey(page.PageId))
                {
                    pageMap.Add(page.PageId, page);
                    page.OnExit();
                }
            }
        }

        public void Show(UIPageId targetPageId)
        {
            if (!pageMap.TryGetValue(targetPageId, out var targetPage))
            {
                Debug.LogWarning($"UI page not found: {targetPageId}");
                return;
            }

            if (currentPage != null && currentPage == targetPage)
            {
                return;
            }

            if (currentPage != null)
            {
                currentPage.OnExit();
            }

            currentPage = targetPage;
            currentPage.OnEnter();
        }

        public void BackTo(UIPageId targetPageId)
        {
            Show(targetPageId);
        }
    }
}

