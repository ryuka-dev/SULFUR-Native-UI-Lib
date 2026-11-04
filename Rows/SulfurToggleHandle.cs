using UnityEngine.UI;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a native checkbox row created by
    /// <see cref="SulfurOptionsContext.AddToggleRow(string, bool, System.Action{bool})"/>.
    /// Lets a page mirror a value it does not own (<see cref="SetValueWithoutNotify"/>),
    /// lock, hide or relabel the row in place, without rebuilding the page.
    /// </summary>
    /// <remarks>
    /// The handle stays valid while the row is hidden and shown again. Once the page is
    /// closed or rebuilt (<see cref="IsAlive"/> false) every member is safe to call and does
    /// nothing useful; take the new handle from the next build.
    /// </remarks>
    public sealed class SulfurToggleHandle
    {
        private readonly SulfurOptionRowParts parts;
        private readonly Toggle toggle;

        internal SulfurToggleHandle(SulfurOptionRowParts parts, Toggle toggle)
        {
            this.parts = parts;
            this.toggle = toggle;
        }

        /// <summary>True while the row exists and the page that built it is on screen.</summary>
        public bool IsAlive
        {
            get { return parts != null && parts.IsAlive; }
        }

        /// <summary>The checkbox state shown now (false when the row is gone).</summary>
        public bool Value
        {
            get { return toggle != null && toggle.isOn; }
        }

        /// <summary>True while <see cref="SetLocked"/>(true) is in effect.</summary>
        public bool IsLocked
        {
            get { return parts != null && parts.IsLocked; }
        }

        /// <summary>Shows <paramref name="value"/> without calling the row's onChanged.</summary>
        public void SetValueWithoutNotify(bool value)
        {
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(value);
        }

        /// <summary>
        /// Locks the row: dimmed as a whole, no mouse input, and keyboard/gamepad Use does
        /// nothing. <see cref="SetValueWithoutNotify"/> still works. Label colours are not touched.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (parts != null)
                parts.SetLocked(locked);
        }

        /// <summary>Shows or hides the row and its description; hidden rows are skipped by keyboard/gamepad navigation.</summary>
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
