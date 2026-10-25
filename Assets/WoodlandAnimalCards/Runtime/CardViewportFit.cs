using UnityEngine;

namespace WoodlandAnimalCards
{
    /// <summary>Uniformly fits this card inside its parent Canvas without changing it.</summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class CardViewportFit : MonoBehaviour
    {
        [Min(0)] public float edgePadding = 24f;
        [Min(0.1f)] public float maximumScale = 1f;
        public bool anchorToLeft;
        public bool respectScreenSafeArea = true;
        private RectTransform ownRect;
        private void OnEnable() { FitNow(); }
        private void LateUpdate() { Fit(); }
        [ContextMenu("Fit Card To Viewport")]
        public void FitNow()
        {
            ownRect = GetComponent<RectTransform>();
            Fit();
        }
        private void Fit()
        {
            if (ownRect == null) return;
            var parent = ownRect.parent as RectTransform;
            if (parent == null || ownRect.rect.width <= 0 || ownRect.rect.height <= 0) return;
            float left = 0, right = 0, bottom = 0, top = 0;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (respectScreenSafeArea && canvas != null && canvas.isRootCanvas &&
                canvas.renderMode == RenderMode.ScreenSpaceOverlay && Screen.width > 0 && Screen.height > 0)
            {
                Rect safe = Screen.safeArea;
                left = safe.xMin / Screen.width * parent.rect.width;
                right = (Screen.width - safe.xMax) / Screen.width * parent.rect.width;
                bottom = safe.yMin / Screen.height * parent.rect.height;
                top = (Screen.height - safe.yMax) / Screen.height * parent.rect.height;
            }
            float width = Mathf.Max(1f, parent.rect.width - left - right - 2f * edgePadding);
            float height = Mathf.Max(1f, parent.rect.height - bottom - top - 2f * edgePadding);
            float scale = Mathf.Min(maximumScale, width / ownRect.rect.width,
                height / ownRect.rect.height);
            ownRect.anchorMin = ownRect.anchorMax = new Vector2(anchorToLeft ? 0f : 0.5f, 0.5f);
            ownRect.pivot = new Vector2(anchorToLeft ? 0f : 0.5f, 0.5f);
            ownRect.anchoredPosition = new Vector2(anchorToLeft ? left + edgePadding : (left - right) / 2f,
                (bottom - top) / 2f);
            ownRect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
