using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Ryuka.Sulfur.NativeUI
{
    // Access to the game's private members. Lookups are resolved once per
    // (runtime type, member name) and cached, including misses, so the ~30 call
    // sites in the options bridge don't repeat a reflection search every call.
    //
    // A miss keeps the old return semantics (default / null / no-op) but logs one
    // warning per (type, member): after a game update renames a private field the
    // log names exactly what broke instead of the UI failing silently. The warning
    // is emitted only when the miss is first cached, so repeated calls stay quiet
    // and allocation-free.
    internal static class SulfurReflection
    {
        private static readonly object cacheLock = new object();
        private static readonly Dictionary<MemberKey, FieldInfo> fieldCache = new Dictionary<MemberKey, FieldInfo>();
        private static readonly Dictionary<MemberKey, MethodInfo> methodCache = new Dictionary<MemberKey, MethodInfo>();

        public static T GetField<T>(object obj, string fieldName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(fieldName))
                return default(T);

            FieldInfo field = ResolveField(obj.GetType(), fieldName);
            if (field == null)
                return default(T);

            object value = field.GetValue(obj);

            if (value is T)
                return (T)value;

            return default(T);
        }

        public static void SetField(object obj, string fieldName, object value)
        {
            if (obj == null || string.IsNullOrWhiteSpace(fieldName))
                return;

            FieldInfo field = ResolveField(obj.GetType(), fieldName);
            if (field == null)
                return;

            field.SetValue(obj, value);
        }

        public static object Invoke(object obj, string methodName, params object[] args)
        {
            if (obj == null || string.IsNullOrWhiteSpace(methodName))
                return null;

            MethodInfo method = ResolveMethod(obj.GetType(), methodName);
            if (method == null)
                return null;

            return method.Invoke(obj, args);
        }

        private static FieldInfo ResolveField(Type type, string name)
        {
            MemberKey key = new MemberKey(type, name);

            lock (cacheLock)
            {
                FieldInfo cached;
                if (fieldCache.TryGetValue(key, out cached))
                    return cached;
            }

            // Resolved outside the lock; a racing duplicate lookup is harmless and the
            // first stored result wins, so the miss warning still fires only once.
            FieldInfo field = AccessTools.Field(type, name);

            lock (cacheLock)
            {
                FieldInfo existing;
                if (fieldCache.TryGetValue(key, out existing))
                    return existing;

                fieldCache[key] = field;
            }

            if (field == null)
                WarnMissing("field", type, name);

            return field;
        }

        private static MethodInfo ResolveMethod(Type type, string name)
        {
            MemberKey key = new MemberKey(type, name);

            lock (cacheLock)
            {
                MethodInfo cached;
                if (methodCache.TryGetValue(key, out cached))
                    return cached;
            }

            MethodInfo method = AccessTools.Method(type, name);

            lock (cacheLock)
            {
                MethodInfo existing;
                if (methodCache.TryGetValue(key, out existing))
                    return existing;

                methodCache[key] = method;
            }

            if (method == null)
                WarnMissing("method", type, name);

            return method;
        }

        private static void WarnMissing(string kind, Type type, string name)
        {
            if (Plugin.Log == null)
                return;

            Plugin.Log.LogWarning(
                "SULFUR Native UI: " + kind + " '" + name + "' not found on " + type.FullName +
                ". The game may have changed; UI that depends on it will not work.");
        }

        private struct MemberKey : IEquatable<MemberKey>
        {
            private readonly Type type;
            private readonly string name;

            public MemberKey(Type type, string name)
            {
                this.type = type;
                this.name = name;
            }

            public bool Equals(MemberKey other)
            {
                return type == other.type && string.Equals(name, other.name, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MemberKey && Equals((MemberKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = type != null ? type.GetHashCode() : 0;
                    return (hash * 397) ^ (name != null ? StringComparer.Ordinal.GetHashCode(name) : 0);
                }
            }
        }
    }
}
