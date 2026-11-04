using PerfectRandom.Sulfur.Core;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// What every native-row handle (<see cref="SulfurToggleHandle"/>,
    /// <see cref="SulfurSliderHandle"/>, <see cref="SulfurCycleHandle"/>) shares: the row,
    /// its binding (which owns the value listener and the lock state), its optional
    /// description row and the context that built it. All members tolerate a destroyed row.
    /// </summary>
    internal sealed class SulfurOptionRowParts
    {
        private readonly SulfurOptionsContext context;
        private readonly OptionsScreenOption option;
        private readonly SulfurOptionBinding binding;
        private readonly GameObject descriptionRow;

        internal SulfurOptionRowParts(
            SulfurOptionsContext context,
            OptionsScreenOption option,
            SulfurOptionBinding binding,
            GameObject descriptionRow)
        {
            this.context = context;
            this.option = option;
            this.binding = binding;
            this.descriptionRow = descriptionRow;
        }

        public SulfurOptionBinding Binding
        {
            get { return binding; }
        }

        public bool IsAlive
        {
            get { return option != null && context != null && context.IsAlive; }
        }

        public bool IsLocked
        {
            get { return binding != null && binding.Locked; }
        }

        public void SetLocked(bool locked)
        {
            if (binding != null)
                binding.SetLocked(locked);
        }

        public void SetVisible(bool visible)
        {
            if (option != null)
                option.gameObject.SetActive(visible);

            if (descriptionRow != null)
                descriptionRow.SetActive(visible);
        }

        public void SetLabel(string label)
        {
            if (option != null)
                option.SetLabel(label ?? "");
        }
    }
}
