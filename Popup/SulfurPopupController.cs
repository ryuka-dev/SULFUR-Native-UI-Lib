using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Persistent, self-built uGUI overlay that renders a single centered message
    /// banner during normal gameplay (HUD surface, independent of the options
    /// screen). It survives scene loads, never pauses the game or captures input,
    /// and costs nothing while hidden (the visual root is deactivated).
    ///
    /// Created lazily on the first <see cref="ShowBanner"/> call so it only exists
    /// once something actually needs it. A single instance is reused; repeated
    /// shows just replace the text.
    /// </summary>
    internal sealed class SulfurPopupController : MonoBehaviour
    {
        // Above virtually all game UI; the vanilla HUD canvases sit far below this.
        private const int OverlaySortingOrder = 32000;

        // SULFUR's signature amber, used for accents/borders. Matches the fallback
        // the OptionsScreen styling uses (section lines, themed-group borders).
        private static readonly Color FallbackAccent = new Color(1f, 0.62f, 0.18f, 1f);

        // Warm near-black panel fill — closer to SULFUR's earthy chrome than pure black.
        private static readonly Color PanelFill = new Color(0.05f, 0.04f, 0.03f, 0.88f);

        private static SulfurPopupController instance;

        private GameObject bannerRoot;     // toggled active/inactive to show/hide
        private TextMeshProUGUI label;
        private bool fontResolved;         // true once we adopt a live, localized game font

        /// <summary>Live instance, bootstrapping one on first access.</summary>
        public static SulfurPopupController Instance
        {
            get
            {
                if (instance == null)
                    Bootstrap();

                return instance;
            }
        }

        /// <summary>
        /// Hide the banner only if a controller already exists. Never bootstraps —
        /// hiding something that was never shown is a no-op.
        /// </summary>
        public static void HideIfExists()
        {
            if (instance != null)
                instance.HideBanner();
        }

        private static void Bootstrap()
        {
            GameObject host = new GameObject("SulfurPopupRoot");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);

            instance = host.AddComponent<SulfurPopupController>();
            instance.Build();
        }

        /// <summary>Show or update the centered banner. Idempotent.</summary>
        public void ShowBanner(string text)
        {
            if (label == null)
                return;

            label.text = text ?? "";

            // Font may only become resolvable once the game's localized HUD exists.
            // Cheap to retry on each show until we lock onto a language-correct font.
            EnsureFont();

            if (bannerRoot != null && !bannerRoot.activeSelf)
                bannerRoot.SetActive(true);
        }

        /// <summary>Hide the banner if shown. Safe when nothing is shown.</summary>
        public void HideBanner()
        {
            if (bannerRoot != null && bannerRoot.activeSelf)
                bannerRoot.SetActive(false);
        }

        private void Build()
        {
            // --- Canvas: screen-space overlay, drawn on top, scales with resolution.
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Intentionally no GraphicRaycaster: the banner must never eat clicks or
            // steal input. All graphics also keep raycastTarget = false.

            // --- Banner root: a centered panel that auto-sizes to its text.
            bannerRoot = new GameObject(
                "Banner",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));

            bannerRoot.transform.SetParent(transform, false);

            RectTransform panelRt = bannerRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            // Slightly above dead-center so the prompt clears the crosshair.
            panelRt.anchoredPosition = new Vector2(0f, 220f);

            Image background = bannerRoot.GetComponent<Image>();
            background.color = PanelFill;
            background.raycastTarget = false;

            HorizontalLayoutGroup layout = bannerRoot.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 20, 20);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = bannerRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- Label.
            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            textObject.transform.SetParent(bannerRoot.transform, false);

            label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = "";
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.fontSize = 34f;
            label.color = new Color(0.94f, 0.90f, 0.84f, 1f); // warm off-white

            // Thin amber rules above and below the text — SULFUR's UI leans on this
            // accent. They stretch to the panel edges and stay out of the layout.
            CreateAccentLine("TopAccent", true);
            CreateAccentLine("BottomAccent", false);

            EnsureFont();

            // Hidden until the first ShowBanner.
            bannerRoot.SetActive(false);
        }

        private Image CreateAccentLine(string name, bool top)
        {
            GameObject line = new GameObject(
                name,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(CanvasRenderer),
                typeof(Image));

            line.transform.SetParent(bannerRoot.transform, false);

            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, 2f);
            rt.anchoredPosition = Vector2.zero;

            // Inset slightly so the rule reads as part of the panel, not its edge.
            rt.offsetMin = new Vector2(10f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-10f, rt.offsetMax.y);

            LayoutElement layout = line.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            Image image = line.GetComponent<Image>();
            image.color = FallbackAccent;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// Adopt the game's current, language-correct TMP font for the label. The
        /// game swaps its font asset per language (e.g. a CJK-capable asset for
        /// Chinese); sampling a live HUD text keeps the banner from rendering as
        /// blank boxes in non-Latin languages. Falls back to the TMP default font
        /// without locking in, so a later show can still upgrade to the real font.
        /// </summary>
        private void EnsureFont()
        {
            if (fontResolved || label == null)
                return;

            TextMeshProUGUI sample = FindLiveGameText();
            if (sample != null && sample.font != null)
            {
                label.font = sample.font;
                label.fontSharedMaterial = sample.fontSharedMaterial;
                label.fontSize = Mathf.Clamp(sample.fontSize * 1.1f, 28f, 48f);
                fontResolved = true;
                return;
            }

            // Temporary fallback: readable now, retried (and replaced) on next show.
            if (label.font == null && TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
        }

        /// <summary>
        /// Find an active, enabled TextMeshProUGUI in the loaded scene to sample the
        /// localized font from. Our own banner label is skipped.
        /// FindObjectsOfType returns only active scene components (not prefab assets),
        /// so this naturally reflects the live, current-language HUD chrome.
        /// </summary>
        private TextMeshProUGUI FindLiveGameText()
        {
            TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            if (texts == null)
                return null;

            foreach (TextMeshProUGUI text in texts)
            {
                if (text == null || text == label || text.font == null)
                    continue;

                return text;
            }

            return null;
        }
    }
}
