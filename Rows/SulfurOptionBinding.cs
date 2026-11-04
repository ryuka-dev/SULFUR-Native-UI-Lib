using System;
using System.Collections.Generic;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    internal sealed class SulfurOptionBinding : MonoBehaviour
    {
        // Share of the row's alpha while it is locked (SetLocked).
        private const float LockedAlphaFactor = 0.45f;

        public Action OnUse;
        public Action<int> OnHorizontal;

        public List<string> CycleValues;
        public int CycleIndex;
        public TextMeshProUGUI CycleLabel;
        public Action<int, string> OnCycleChanged;

        // The row's own listener on the game's Toggle / Slider. OptionsScreenOption.OnDisable
        // calls RemoveAllListeners() on both, so a row hidden and shown again (its own
        // SetActive, or an ancestor's: a setting handle, a list, a group) would keep moving
        // the control but never report the change. The binding owns the listener and puts
        // it back on every enable.
        private Toggle boundToggle;
        private UnityAction<bool> toggleListener;
        private UnityAction<bool> toggleHandler;
        private Slider boundSlider;
        private UnityAction<float> sliderListener;
        private UnityAction<float> sliderHandler;

        // Slider value the row last reported (or was set to without notify). Mouse drags are
        // snapped to SliderSnapStep when it is > 0, and only a change of the snapped value
        // is reported.
        private float sliderLastValue;
        private float sliderSnapStep;

        private CanvasGroup lockGroup;
        private float unlockedAlpha = 1f;
        private bool unlockedInteractable = true;
        private bool unlockedBlocksRaycasts = true;

        // The game's own selection flag (OptionsScreenOption.isSelected), read directly. A
        // Harmony postfix on SetSelected is not reliable: the method is two lines long and
        // the JIT inlines it into SelectIndex, so the patch is skipped on that path.
        private static readonly AccessTools.FieldRef<OptionsScreenOption, bool> isSelectedRef = ResolveIsSelected();
        private OptionsScreenOption option;

        /// <summary>True while the game has this row selected (keyboard/gamepad focus).</summary>
        public bool IsSelected
        {
            get
            {
                if (isSelectedRef == null)
                    return false;

                if (option == null)
                    option = GetComponent<OptionsScreenOption>();

                return option != null && isSelectedRef(option);
            }
        }

        private static AccessTools.FieldRef<OptionsScreenOption, bool> ResolveIsSelected()
        {
            try
            {
                return AccessTools.FieldRefAccess<OptionsScreenOption, bool>("isSelected");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning(
                    "SULFUR Native UI: OptionsScreenOption.isSelected not found (" + e.Message +
                    "); button rows will not show which button has keyboard/gamepad focus.");
                return null;
            }
        }

        /// <summary>True while the row is locked: Use, horizontal moves and value changes are ignored.</summary>
        public bool Locked { get; private set; }

        public void BindToggle(Toggle toggle, UnityAction<bool> listener)
        {
            DetachListeners();
            boundToggle = toggle;
            toggleListener = listener;

            if (toggleHandler == null)
                toggleHandler = HandleToggleChanged;

            AttachListeners();
        }

        public void BindSlider(Slider slider, UnityAction<float> listener, float snapStep)
        {
            DetachListeners();
            boundSlider = slider;
            sliderListener = listener;
            sliderSnapStep = snapStep > 0f ? snapStep : 0f;
            sliderLastValue = slider != null ? slider.value : 0f;

            if (sliderHandler == null)
                sliderHandler = HandleSliderChanged;

            AttachListeners();
        }

        private void OnEnable()
        {
            AttachListeners();
        }

        // Remove first: the handler is added at most once, whether or not the game's
        // OnDisable cleared it since the last attach.
        private void AttachListeners()
        {
            if (boundToggle != null && toggleHandler != null)
            {
                boundToggle.onValueChanged.RemoveListener(toggleHandler);
                boundToggle.onValueChanged.AddListener(toggleHandler);
            }

            if (boundSlider != null && sliderHandler != null)
            {
                boundSlider.onValueChanged.RemoveListener(sliderHandler);
                boundSlider.onValueChanged.AddListener(sliderHandler);
            }
        }

        private void DetachListeners()
        {
            if (boundToggle != null && toggleHandler != null)
                boundToggle.onValueChanged.RemoveListener(toggleHandler);

            if (boundSlider != null && sliderHandler != null)
                boundSlider.onValueChanged.RemoveListener(sliderHandler);
        }

        private void HandleToggleChanged(bool state)
        {
            if (Locked)
            {
                // Something still flipped a locked checkbox (e.g. the game's Use when its
                // IsLocked field could not be set): put it back without reporting.
                if (boundToggle != null)
                    boundToggle.SetIsOnWithoutNotify(!state);

                return;
            }

            if (toggleListener != null)
                toggleListener(state);
        }

        private void HandleSliderChanged(float value)
        {
            if (boundSlider == null)
                return;

            if (Locked)
            {
                boundSlider.SetValueWithoutNotify(sliderLastValue);
                return;
            }

            if (sliderSnapStep > 0f)
            {
                float snapped = SnapSliderValue(value);
                if (snapped != value)
                    boundSlider.SetValueWithoutNotify(snapped);

                // Within one step the snapped value does not change: nothing to report.
                if (Mathf.Approximately(boundSlider.value, sliderLastValue))
                    return;

                value = boundSlider.value;
            }

            sliderLastValue = value;

            if (sliderListener != null)
                sliderListener(value);
        }

        private float SnapSliderValue(float value)
        {
            float min = boundSlider.minValue;
            float max = boundSlider.maxValue;
            float snapped = min + Mathf.Round((value - min) / sliderSnapStep) * sliderSnapStep;
            return Mathf.Clamp(snapped, min, max);
        }

        /// <summary>Moves the bound slider without reporting the change (also not later, as a drag delta).</summary>
        public void SetSliderValueWithoutNotify(float value)
        {
            if (boundSlider == null)
                return;

            boundSlider.SetValueWithoutNotify(value);
            sliderLastValue = boundSlider.value;
        }

        public void InvokeUse()
        {
            if (Locked)
                return;

            if (OnUse != null)
                OnUse();
        }

        public void InvokeHorizontal(int delta)
        {
            if (Locked)
                return;

            if (OnHorizontal != null)
                OnHorizontal(delta);
        }

        public void MoveCycle(int delta)
        {
            if (Locked || CycleValues == null || CycleValues.Count == 0)
                return;

            CycleIndex += delta;

            if (CycleIndex < 0)
                CycleIndex = CycleValues.Count - 1;

            if (CycleIndex >= CycleValues.Count)
                CycleIndex = 0;

            string value = CycleValues[CycleIndex];

            if (CycleLabel != null)
                CycleLabel.text = value;

            if (OnCycleChanged != null)
                OnCycleChanged(CycleIndex, value);
        }

        /// <summary>Shows the value at <paramref name="index"/> (clamped) without reporting it.</summary>
        public void SetCycleIndexWithoutNotify(int index)
        {
            if (CycleValues == null || CycleValues.Count == 0)
                return;

            CycleIndex = Mathf.Clamp(index, 0, CycleValues.Count - 1);

            if (CycleLabel != null)
                CycleLabel.text = CycleValues[CycleIndex];
        }

        /// <summary>
        /// Locks or unlocks the row. A CanvasGroup on the row dims it uniformly and blocks
        /// pointer input; the game's own IsLocked makes its Use a no-op; the binding ignores
        /// Use, horizontal moves and value changes while locked. Label colours are never
        /// touched: the game's SetLocked recolours the label with serialized colours that
        /// are transparent on library-built rows.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (lockGroup == null)
            {
                // The row's own CanvasGroup when the prefab has one on its root (its values
                // are restored on unlock), else one of our own.
                lockGroup = GetComponent<CanvasGroup>();
                if (lockGroup == null)
                    lockGroup = gameObject.AddComponent<CanvasGroup>();

                unlockedAlpha = lockGroup.alpha;
                unlockedInteractable = lockGroup.interactable;
                unlockedBlocksRaycasts = lockGroup.blocksRaycasts;
            }
            else if (!Locked)
            {
                unlockedAlpha = lockGroup.alpha;
                unlockedInteractable = lockGroup.interactable;
                unlockedBlocksRaycasts = lockGroup.blocksRaycasts;
            }

            if (Locked == locked)
                return;

            Locked = locked;

            OptionsScreenOption option = GetComponent<OptionsScreenOption>();
            if (option != null)
                SulfurReflection.SetField(option, "IsLocked", locked);

            if (locked)
            {
                lockGroup.alpha = unlockedAlpha * LockedAlphaFactor;
                lockGroup.interactable = false;
                lockGroup.blocksRaycasts = false;
            }
            else
            {
                lockGroup.alpha = unlockedAlpha;
                lockGroup.interactable = unlockedInteractable;
                lockGroup.blocksRaycasts = unlockedBlocksRaycasts;
            }
        }
    }
}
