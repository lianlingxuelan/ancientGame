using UnityEngine;

namespace Shouyou.UI
{
    public abstract class UIPageBase : MonoBehaviour
    {
        [SerializeField] private UIPageId pageId;

        public UIPageId PageId => pageId;

        public virtual void OnEnter()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnExit()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnPause()
        {
        }

        public virtual void OnResume()
        {
        }
    }
}

