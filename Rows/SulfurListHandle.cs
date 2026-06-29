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
            if (container == null)
                return;

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

        /// <summary>
        /// Clears the list and rebuilds it by running <paramref name="build"/> with the
        /// owning context redirected into this list's container. Compose rows with the
        /// usual <c>ctx.AddTextRow</c> / <c>ctx.AddBadgeRow</c> / <c>ctx.AddButtonRow</c> etc.
        /// </summary>
        public void Update(Action<SulfurOptionsContext> build)
        {
            if (container == null || context == null)
                return;

            Clear();

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

        public void SetVisible(bool visible)
        {
            if (container != null)
                container.gameObject.SetActive(visible);
        }
    }
}
