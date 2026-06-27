namespace Ryuka.Sulfur.NativeUI
{
    public static class SulfurOptionsStateExtensions
    {
        public static T GetState<T>(
            this SulfurOptionsContext ctx,
            string key,
            T defaultValue)
        {
            if (ctx == null)
                return defaultValue;

            return SulfurPageStateStore.Get(ctx.PageId, key, defaultValue);
        }

        public static void SetState<T>(
            this SulfurOptionsContext ctx,
            string key,
            T value)
        {
            if (ctx == null)
                return;

            SulfurPageStateStore.Set(ctx.PageId, key, value);
        }

        public static bool HasState(
            this SulfurOptionsContext ctx,
            string key)
        {
            if (ctx == null)
                return false;

            return SulfurPageStateStore.Has(ctx.PageId, key);
        }

        public static void RemoveState(
            this SulfurOptionsContext ctx,
            string key)
        {
            if (ctx == null)
                return;

            SulfurPageStateStore.Remove(ctx.PageId, key);
        }

        public static void ClearPageState(this SulfurOptionsContext ctx)
        {
            if (ctx == null)
                return;

            SulfurPageStateStore.ClearPage(ctx.PageId);
        }

        public static bool ToggleBoolState(
            this SulfurOptionsContext ctx,
            string key,
            bool defaultValue)
        {
            bool value = ctx.GetState(key, defaultValue);
            value = !value;
            ctx.SetState(key, value);
            return value;
        }
    }
}
