namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a native alternative ("&lt; value &gt;") row created by
    /// <see cref="SulfurOptionsContext.AddCycleRow(string, System.Collections.Generic.IReadOnlyList{string}, int, System.Action{int, string})"/>.
    /// Same shape as <see cref="SulfurToggleHandle"/>; the value is an index into the row's values.
    /// </summary>
    public sealed class SulfurCycleHandle
    {
        private readonly SulfurOptionRowParts parts;

        internal SulfurCycleHandle(SulfurOptionRowParts parts)
        {
            this.parts = parts;
        }

        private SulfurOptionBinding Binding
        {
            get { return parts != null ? parts.Binding : null; }
        }

        /// <summary>True while the row exists and the page that built it is on screen.</summary>
        public bool IsAlive
        {
            get { return parts != null && parts.IsAlive; }
        }

        /// <summary>Index of the value shown now (0 when the row is gone).</summary>
        public int Index
        {
            get { return Binding != null ? Binding.CycleIndex : 0; }
        }

        /// <summary>The value shown now ("" when the row is gone).</summary>
        public string Value
        {
            get
            {
                SulfurOptionBinding binding = Binding;
                if (binding == null || binding.CycleValues == null || binding.CycleValues.Count == 0)
                    return "";

                return binding.CycleValues[binding.CycleIndex] ?? "";
            }
        }

        /// <summary>True while <see cref="SetLocked"/>(true) is in effect.</summary>
        public bool IsLocked
        {
            get { return parts != null && parts.IsLocked; }
        }

        /// <summary>Shows the value at <paramref name="index"/> (clamped) without calling the row's onChanged.</summary>
        public void SetIndexWithoutNotify(int index)
        {
            SulfurOptionBinding binding = Binding;
            if (binding != null)
                binding.SetCycleIndexWithoutNotify(index);
        }

        /// <summary>
        /// Locks the row: dimmed as a whole, no mouse input on its arrows, and keyboard/gamepad
        /// left/right does nothing. <see cref="SetIndexWithoutNotify"/> still works.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (parts != null)
                parts.SetLocked(locked);
        }

        /// <summary>Shows or hides the row and its description.</summary>
        public void SetVisible(bool visible)
        {
            if (parts != null)
                parts.SetVisible(visible);
        }

        public void SetLabel(string label)
        {
            if (parts != null)
                parts.SetLabel(label);
        }
    }
}
