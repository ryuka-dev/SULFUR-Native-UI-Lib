using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Persistent, self-built uGUI overlay that stacks transient message toasts in
    /// the top-right corner during normal gameplay. Survives scene loads, never
    /// pauses the game or captures input, and drives all card animations on
    /// unscaled time so motion stays smooth regardless of game time scale.
    ///
    /// Created lazily on the first <see cref="Show"/>. Newest toast sits on top;
    /// older ones glide down to make room and slide out when they expire.
    /// </summary>
    internal sealed class SulfurToastController : MonoBehaviour
    {
        // Layout constants are referenced by SulfurToastView for its slide maths.
        public const float CardWidth = 360f;
        public const float RightMargin = 24f;

        private const float TopMargin = 24f;
        private const float CardSpacing = 12f;
        private const int MaxVisible = 5;

        // Above the banner overlay so a toast is never hidden behind it.
        private const int OverlaySortingOrder = 32050;

        private const float DefaultDuration = 4f;

        private static SulfurToastController instance;

        // Index 0 = newest = top of the stack.
        private readonly List<SulfurToastView> toasts = new List<SulfurToastView>();

        private bool fontResolved;
        private TMP_FontAsset font;
        private Material fontMaterial;

        public static SulfurToastController Instance
        {
            get
            {
                if (instance == null)
                    Bootstrap();

                return instance;
            }
        }

        private static void Bootstrap()
        {
            GameObject host = new GameObject("SulfurToastRoot");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);

            Canvas canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Intentionally no GraphicRaycaster — toasts must never intercept input.

            instance = host.AddComponent<SulfurToastController>();
        }

        public void Show(string title, string message, float durationSeconds)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
                return;

            EnsureFont();

            float duration = durationSeconds > 0f ? durationSeconds : DefaultDuration;

            SulfurToastView view = SulfurToastView.Create(
                transform, font, fontMaterial, title, message, duration);

            toasts.Insert(0, view);

            EnforceLimit();
        }

        // Keep at most MaxVisible cards on screen by retiring the oldest still-visible
        // ones early. They animate out cleanly rather than popping.
        private void EnforceLimit()
        {
            int visible = 0;
            foreach (SulfurToastView toast in toasts)
            {
                if (!toast.IsLeaving)
                    visible++;
            }

            for (int i = toasts.Count - 1; i >= 0 && visible > MaxVisible; i--)
            {
                if (!toasts[i].IsLeaving)
                {
                    toasts[i].BeginExit();
                    visible--;
                }
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Assign each still-present card its target slot, top to bottom. Leaving
            // cards are skipped so the stack closes the gap above them immediately.
            float y = -TopMargin;
            for (int i = 0; i < toasts.Count; i++)
            {
                SulfurToastView toast = toasts[i];
                if (toast.IsLeaving)
                    continue;

                toast.SetTargetY(y);
                y -= toast.Height + CardSpacing;
            }

            for (int i = 0; i < toasts.Count; i++)
                toasts[i].Tick(dt);

            // Reap finished cards.
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (toasts[i].IsDead)
                {
                    toasts[i].Destroy();
                    toasts.RemoveAt(i);
                }
            }
        }

        private void EnsureFont()
        {
            if (fontResolved)
                return;

            TextMeshProUGUI sample = SulfurUiFont.FindLiveGameText(transform);
            if (sample != null && sample.font != null)
            {
                font = sample.font;
                fontMaterial = sample.fontSharedMaterial;
                fontResolved = true;
                return;
            }

            // Temporary fallback: readable now, retried (and upgraded) on next Show.
            if (font == null && TMP_Settings.defaultFontAsset != null)
                font = TMP_Settings.defaultFontAsset;
        }
    }
}
