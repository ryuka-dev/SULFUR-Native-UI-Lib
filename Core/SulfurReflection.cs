using System;
using System.Reflection;
using HarmonyLib;

namespace Ryuka.Sulfur.NativeUI
{
    internal static class SulfurReflection
    {
        public static T GetField<T>(object obj, string fieldName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(fieldName))
                return default(T);

            FieldInfo field = AccessTools.Field(obj.GetType(), fieldName);
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

            FieldInfo field = AccessTools.Field(obj.GetType(), fieldName);
            if (field == null)
                return;

            field.SetValue(obj, value);
        }

        public static object Invoke(object obj, string methodName, params object[] args)
        {
            if (obj == null || string.IsNullOrWhiteSpace(methodName))
                return null;

            MethodInfo method = AccessTools.Method(obj.GetType(), methodName);
            if (method == null)
                return null;

            return method.Invoke(obj, args);
        }
    }
}