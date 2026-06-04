using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    internal sealed class SulfurOptionBinding : MonoBehaviour
    {
        public Action OnUse;
        public Action<int> OnHorizontal;

        public List<string> CycleValues;
        public int CycleIndex;
        public TextMeshProUGUI CycleLabel;
        public Action<int, string> OnCycleChanged;

        public void InvokeUse()
        {
            if (OnUse != null)
                OnUse();
        }

        public void InvokeHorizontal(int delta)
        {
            if (OnHorizontal != null)
                OnHorizontal(delta);
        }

        public void MoveCycle(int delta)
        {
            if (CycleValues == null || CycleValues.Count == 0)
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
    }
}
