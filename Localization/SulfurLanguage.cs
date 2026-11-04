using System;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// The game's current UI language, and a single notification when it becomes known or
    /// changes. Use this instead of reading I2 Localization or subscribing to the game's
    /// <c>onLanguageChange</c> yourself.
    ///
    /// <para><b>Startup.</b> The game sets its startup language without raising any event.
    /// The library checks once per frame (from its plugin's Update) until the game has finished
    /// loading and reports a language code, then stops checking and raises
    /// <see cref="Changed"/> once. Until then <see cref="IsReady"/> is false and
    /// <see cref="Code"/> is <c>"en"</c>.</para>
    ///
    /// <para><b>Changes.</b> <see cref="Changed"/> is raised after the game has committed a
    /// language change and after all of the game's own language-change handlers have run, so
    /// text set from a handler is not overwritten by the game. It is raised only when the code
    /// actually changed.</para>
    ///
    /// <para>All notifications are raised on the Unity main thread. Every subscriber runs in
    /// its own try/catch; an exception is logged as a warning and does not stop the others.</para>
    /// </summary>
    public static class SulfurLanguage
    {
        private const string DefaultCode = "en";

        private static string code = DefaultCode;
        private static bool ready;
        private static bool rightToLeft;
        private static bool initialized;
        private static bool raising;
        private static bool raisePending;

        /// <summary>
        /// The game's current language code as I2 Localization reports it: <c>"en"</c>,
        /// <c>"zh-CN"</c>, <c>"ja"</c>, ... These are also the <c>lang/&lt;code&gt;.json</c> file
        /// names. <c>"en"</c> until <see cref="IsReady"/>.
        /// </summary>
        public static string Code
        {
            get { return code; }
        }

        /// <summary>True once the game's language is known.</summary>
        public static bool IsReady
        {
            get { return ready; }
        }

        /// <summary>True when the current language is written right to left (Arabic).</summary>
        public static bool IsRightToLeft
        {
            get { return rightToLeft; }
        }

        /// <summary>
        /// Raised on the main thread once when the language first becomes known, then after
        /// every change. The argument is the new <see cref="Code"/>.
        /// </summary>
        public static event Action<string> Changed;

        /// <summary>
        /// Shapes <paramref name="text"/> for display when the current language is right to
        /// left, exactly as the game does for its own translations; otherwise returns it
        /// unchanged. Apply it once when the text is resolved, not on every frame.
        /// </summary>
        public static string FixRtl(string text)
        {
            if (!rightToLeft || string.IsNullOrEmpty(text))
                return text;

            return SulfurGameLanguage.FixRtl(text);
        }

        /// <summary>
        /// Calls <paramref name="onLanguage"/> with the current code now if the language is
        /// already known (otherwise when it first becomes known), then after every change.
        /// Dispose the result to stop. Exceptions thrown by the callback are logged, not
        /// propagated.
        /// </summary>
        public static IDisposable Follow(Action<string> onLanguage)
        {
            if (onLanguage == null)
                throw new ArgumentNullException("onLanguage");

            Subscription subscription = new Subscription(onLanguage);
            Changed += subscription.Handler;

            if (ready)
            {
                try
                {
                    subscription.Handler(code);
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogWarning("Language: a language follower (" + Describe(subscription.Handler) +
                                           ") failed: " + e);
                }
            }

            return subscription;
        }

        /// <summary>
        /// Starts listening to the game's language commits. Called once from Plugin.Awake,
        /// after <see cref="SulfurGameLanguage.Install"/>. Idempotent.
        /// </summary>
        internal static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            SulfurGameLanguage.LanguageCommitted += OnGameLanguageCommitted;
        }

        /// <summary>
        /// Startup convergence: checks whether the game's startup language is known yet.
        /// No-op once ready. Called from the plugin's Update; cheap when the game is still
        /// loading (one property read).
        /// </summary>
        internal static void ConvergeInitial()
        {
            if (ready)
                return;

            string gameCode;
            bool gameRightToLeft;

            if (SulfurGameLanguage.TryRead(out gameCode, out gameRightToLeft))
            {
                Apply(gameCode, gameRightToLeft);
                return;
            }

            // Deterministic fallback: the game's language cannot be read on this build, so
            // report English once instead of leaving followers waiting forever.
            if (SulfurGameLanguage.Unsupported)
                Apply(DefaultCode, false);
        }

        private static void OnGameLanguageCommitted()
        {
            if (!ready)
            {
                // A change before startup finished is picked up by the startup check.
                ConvergeInitial();
                return;
            }

            string gameCode;
            bool gameRightToLeft;

            if (SulfurGameLanguage.TryRead(out gameCode, out gameRightToLeft))
                Apply(gameCode, gameRightToLeft);
        }

        private static void Apply(string newCode, bool newRightToLeft)
        {
            bool wasReady = ready;

            if (wasReady && string.Equals(code, newCode, StringComparison.Ordinal))
            {
                rightToLeft = newRightToLeft;
                return;
            }

            code = newCode;
            rightToLeft = newRightToLeft;
            ready = true;

            // The library's own lookup table follows first, so subscribers that call
            // SulfurLocalization.Get already see the new language.
            SulfurLocalization.OnLanguageChanged(newCode);

            Plugin.Log?.LogInfo("Language: " + (wasReady ? "changed to " : "detected ") + newCode +
                                (newRightToLeft ? " (right to left)" : "") + ".");

            Raise(newCode);
        }

        private static void Raise(string newCode)
        {
            if (raising)
            {
                // A subscriber changed the language from inside its own notification. Finish
                // the current round, then notify everyone again with the newer code.
                raisePending = true;
                return;
            }

            raising = true;
            try
            {
                string delivering = newCode;

                // Bounded: subscribers that keep switching the language must not hang the game.
                for (int round = 0; round < 4; round++)
                {
                    raisePending = false;
                    RaiseOnce(delivering);

                    if (!raisePending)
                        break;

                    delivering = code;
                }
            }
            finally
            {
                raising = false;
                raisePending = false;
            }
        }

        private static void RaiseOnce(string newCode)
        {
            Action<string> handlers = Changed;
            if (handlers == null)
                return;

            foreach (Delegate d in handlers.GetInvocationList())
            {
                Action<string> handler = (Action<string>)d;
                try
                {
                    handler(newCode);
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogWarning("Language: a language-change subscriber (" + Describe(handler) +
                                           ") failed: " + e);
                }
            }
        }

        private static string Describe(Action<string> handler)
        {
            Subscription subscription = handler.Target as Subscription;
            Delegate target = subscription != null ? subscription.Callback : handler;

            if (target.Method == null)
                return "unknown";

            Type type = target.Method.DeclaringType;
            return (type != null ? type.FullName + "." : "") + target.Method.Name;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action<string> callback;
            private bool disposed;

            public Subscription(Action<string> callback)
            {
                this.callback = callback;
                Handler = Invoke;
            }

            public Action<string> Handler { get; private set; }

            public Action<string> Callback
            {
                get { return callback; }
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                Changed -= Handler;
            }

            private void Invoke(string languageCode)
            {
                if (disposed)
                    return;

                callback(languageCode);
            }
        }
    }
}
