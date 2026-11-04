using System;
using UnityEngine;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// Runtime handle for a refreshable list section created by
    /// <see cref="SulfurOptionsContext.AddList"/>. Call <see cref="Update"/> with a
    /// builder that composes rows using the normal Add* API; the section is cleared
    /// and rebuilt in place, without rebuilding the whole options page (e.g. a live
    /// player/ping table).
    /// </summary>
    /// <remarks>
    /// The list owns every row under its container. Native option rows built into it
    /// are part of the game's keyboard/gamepad navigation list: clearing removes them
    /// from it, and rebuilt rows are inserted at the list's place in the page, so
    /// navigation order stays equal to screen order.
    /// <para>Once the page that created the list is closed or rebuilt (its context is no
    /// longer <see cref="SulfurOptionsContext.IsAlive"/>), <see cref="Clear"/> and
    /// <see cref="Update"/> do nothing, so a handle kept by a timer can be updated safely.</para>
    /// </remarks>
    public sealed class SulfurListHandle
    {
        private readonly SulfurOptionsContext context;
        private readonly RectTransform container;

        internal SulfurListHandle(SulfurOptionsContext context, RectTransform container)
        {
            this.context = context;
            this.container = container;
        }

        /// <summary>Removes all rows currently in the list.</summary>
        public void Clear()
        {
            if (container == null || context == null || !context.IsAlive)
                return;

            int previousSelectedIndex = context != null ? context.CaptureSelectedOptionIndex() : -1;

            ClearRows();

            if (context != null)
                context.ReconcileSelectedOption(previousSelectedIndex);
        }

        /// <summary>
        /// Clears the list and rebuilds it by running <paramref name="build"/> with the
        /// owning context redirected into this list's container. Compose rows with the
        /// usual <c>ctx.AddTextRow</c> / <c>ctx.AddBadgeRow</c> / <c>ctx.AddButtonRow</c> etc.
        /// </summary>
        public void Update(Action<SulfurOptionsContext> build)
        {
            if (container == null || context == null || !context.IsAlive)
                return;

            // Selection is reconciled once, after the rebuild, so a cursor that was on a
            // list row lands on the rebuilt row at the same position.
            int previousSelectedIndex = context.CaptureSelectedOptionIndex();

            try
            {
                ClearRows();

                if (build == null)
                    return;

                context.PushContainer(container);
                try
                {
                    build(context);
                }
                finally
                {
                    context.PopContainer();
                }
            }
            finally
            {
                context.ReconcileSelectedOption(previousSelectedIndex);
            }
        }

        public void SetVisible(bool visible)
        {
            if (container != null)
                container.gameObject.SetActive(visible);
        }

        private void ClearRows()
        {
            // Take the rows out of the game's navigation list first: Destroy is deferred,
            // but the game indexes that list directly and would hit the destroyed rows.
            if (context != null)
                context.UnregisterOptionsUnder(container);

            // Detach before destroying so the old rows leave layout immediately and
            // don't double up with freshly-added rows for a frame.
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child == null)
                    continue;

                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
