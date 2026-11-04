using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Keyboard/gamepad focus inside one <see cref="SulfurOptionsContext.AddButtonRow(SulfurButton[])"/>
    /// row. Lives on the row's navigation proxy: left/right moves the focus between the
    /// row's buttons (skipping hidden and non-interactable ones, no wrap-around), Use presses
    /// the focused one.
    /// </summary>
    /// <remarks>
    /// EventSystem navigation is off on the options screen and the buttons' Selected tint is
    /// practically their normal colour, so the focus is drawn by the library: a thin white
    /// frame (the colour of the game's own selection border) around the focused button,
    /// shown only while the row is the game's selected row.
    /// </remarks>
    internal sealed class SulfurButtonRowNavigator : MonoBehaviour
    {
        private const float FrameThickness = 2f;

        private readonly List<Button> buttons = new List<Button>();
        private readonly List<GameObject> frames = new List<GameObject>();
        private SulfurOptionBinding binding;
        private int focusIndex;
        private int shownIndex = -1;

        public void Configure(SulfurOptionBinding owner, IList<Button> rowButtons)
        {
            binding = owner;
            buttons.Clear();
            frames.Clear();

            if (rowButtons == null)
                return;

            foreach (Button button in rowButtons)
            {
                if (button == null)
                    continue;

                buttons.Add(button);
                frames.Add(CreateFocusFrame(button.transform));
            }

            focusIndex = 0;
        }

        public void Move(int delta)
        {
            if (delta == 0 || buttons.Count == 0)
                return;

            int step = delta > 0 ? 1 : -1;
            int start = ResolveFocus();
            if (start < 0)
                return;

            for (int i = start + step; i >= 0 && i < buttons.Count; i += step)
            {
                if (IsUsable(i))
                {
                    focusIndex = i;
                    break;
                }
            }

            Refresh();
        }

        public void UseFocused()
        {
            int index = ResolveFocus();
            if (index < 0)
                return;

            SulfurNavigationProxy.Press(buttons[index]);
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void OnDisable()
        {
            ShowFrame(-1);
        }

        private void Refresh()
        {
            int wanted = binding != null && binding.IsSelected ? ResolveFocus() : -1;
            if (wanted != shownIndex)
                ShowFrame(wanted);
        }

        // The focused button, or the nearest usable one after/before it when it was hidden
        // or disabled since; -1 when none is usable.
        private int ResolveFocus()
        {
            if (buttons.Count == 0)
                return -1;

            focusIndex = Mathf.Clamp(focusIndex, 0, buttons.Count - 1);

            if (IsUsable(focusIndex))
                return focusIndex;

            for (int i = focusIndex + 1; i < buttons.Count; i++)
            {
                if (IsUsable(i))
                    return focusIndex = i;
            }

            for (int i = focusIndex - 1; i >= 0; i--)
            {
                if (IsUsable(i))
                    return focusIndex = i;
            }

            return -1;
        }

        private bool IsUsable(int index)
        {
            Button button = buttons[index];
            return button != null && button.gameObject.activeInHierarchy && button.IsInteractable();
        }

        private void ShowFrame(int index)
        {
            if (shownIndex >= 0 && shownIndex < frames.Count && frames[shownIndex] != null)
                frames[shownIndex].SetActive(false);

            shownIndex = -1;

            if (index >= 0 && index < frames.Count && frames[index] != null)
            {
                frames[index].SetActive(true);
                shownIndex = index;
            }
        }

        private static GameObject CreateFocusFrame(Transform button)
        {
            GameObject frame = new GameObject("SULFUR_FocusFrame", typeof(RectTransform), typeof(LayoutElement));
            frame.transform.SetParent(button, false);

            RectTransform rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            frame.GetComponent<LayoutElement>().ignoreLayout = true;

            float t = FrameThickness;
            CreateLine(frame.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(t, 0f));
            CreateLine(frame.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-t, 0f), new Vector2(0f, 0f));
            CreateLine(frame.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(t, -t), new Vector2(-t, 0f));
            CreateLine(frame.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(t, 0f), new Vector2(-t, t));

            frame.SetActive(false);
            return frame;
        }

        private static void CreateLine(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject line = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            line.transform.SetParent(parent, false);

            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image image = line.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }
}
