using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// The value side of one library number input (stacked, inline or setting row): parses
    /// the text when editing ends, clamps and rounds it, reports it, and remembers the last
    /// accepted value that unparseable input reverts to. One instance per field; the field's
    /// onEndEdit is owned by it.
    /// </summary>
    /// <remarks>
    /// A float field keeps the arithmetic of the original float API (float parse, Mathf.Clamp,
    /// Mathf.Round on value * 10^decimals), so its behaviour is unchanged. A double field
    /// parses, rounds and formats in double with the invariant culture; its text shows up to
    /// 15 significant digits (the precision the runtime formats doubles with), which is also
    /// the number of decimal digits a double holds exactly.
    /// </remarks>
    internal sealed class SulfurNumberInput
    {
        // Math.Round(double, int) accepts 0..15 digits.
        private const int MaxDoubleDecimals = 15;

        private readonly TMP_InputField input;
        private readonly bool isDouble;
        private readonly int decimals;

        private readonly float floatMin;
        private readonly float floatMax;
        private readonly Action<float> floatChanged;
        private float floatLastAccepted;

        private readonly double doubleMin;
        private readonly double doubleMax;
        private readonly Action<double> doubleChanged;
        private double doubleLastAccepted;

        private SulfurNumberInput(TMP_InputField input, float value, float min, float max, int decimals, Action<float> onChanged)
        {
            this.input = input;
            this.decimals = decimals;
            floatMin = min;
            floatMax = max;
            floatChanged = onChanged;
            floatLastAccepted = value;
        }

        private SulfurNumberInput(TMP_InputField input, double value, double min, double max, int decimals, Action<double> onChanged)
        {
            this.input = input;
            this.decimals = decimals;
            isDouble = true;
            doubleMin = min;
            doubleMax = max;
            doubleChanged = onChanged;
            doubleLastAccepted = value;
        }

        /// <summary>True for a field built with the double API.</summary>
        public bool IsDouble
        {
            get { return isDouble; }
        }

        /// <summary>Takes over <paramref name="field"/>'s onEndEdit for a float value.</summary>
        public static SulfurNumberInput AttachFloat(TMP_InputField field, float value, float min, float max, int decimals, Action<float> onChanged)
        {
            SulfurNumberInput model = new SulfurNumberInput(field, value, min, max, decimals, onChanged);
            model.Attach();
            return model;
        }

        /// <summary>Takes over <paramref name="field"/>'s onEndEdit for a double value.</summary>
        public static SulfurNumberInput AttachDouble(TMP_InputField field, double value, double min, double max, int decimals, Action<double> onChanged)
        {
            SulfurNumberInput model = new SulfurNumberInput(field, value, min, max, decimals, onChanged);
            model.Attach();
            return model;
        }

        /// <summary>
        /// Shows <paramref name="value"/> the way the field shows its build value (formatted
        /// with the field's decimals, not clamped) and makes it the value unparseable input
        /// reverts to. Nothing is reported. A float field stores it as float. False when the
        /// field is gone.
        /// </summary>
        public bool SetValueWithoutNotify(double value)
        {
            if (input == null)
                return false;

            if (isDouble)
            {
                doubleLastAccepted = value;
                input.SetTextWithoutNotify(FormatDouble(value, decimals));
            }
            else
            {
                float f = (float)value;
                floatLastAccepted = f;
                input.SetTextWithoutNotify(FormatFloat(f, decimals));
            }

            return true;
        }

        private void Attach()
        {
            if (input == null)
                return;

            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(new UnityAction<string>(HandleEndEdit));
        }

        private void HandleEndEdit(string text)
        {
            if (isDouble)
                HandleDoubleEndEdit(text);
            else
                HandleFloatEndEdit(text);
        }

        // The original float path, unchanged.
        private void HandleFloatEndEdit(string text)
        {
            float parsed;
            if (!TryParseFloat(text, out parsed))
            {
                input.SetTextWithoutNotify(FormatFloat(floatLastAccepted, decimals));
                return;
            }

            parsed = Mathf.Clamp(parsed, floatMin, floatMax);
            parsed = RoundFloat(parsed, decimals);
            floatLastAccepted = parsed;

            input.SetTextWithoutNotify(FormatFloat(parsed, decimals));

            if (floatChanged != null)
                floatChanged(parsed);
        }

        private void HandleDoubleEndEdit(string text)
        {
            double parsed;
            if (!TryParseDouble(text, out parsed))
            {
                input.SetTextWithoutNotify(FormatDouble(doubleLastAccepted, decimals));
                return;
            }

            parsed = ClampDouble(parsed, doubleMin, doubleMax);
            parsed = RoundDouble(parsed, decimals);
            doubleLastAccepted = parsed;

            input.SetTextWithoutNotify(FormatDouble(parsed, decimals));

            if (doubleChanged != null)
                doubleChanged(parsed);
        }

        // --- float (original behaviour) -----------------------------------

        internal static bool TryParseFloat(string text, out float value)
        {
            value = 0f;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string normalized = text.Trim().Replace(',', '.');

            return float.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        internal static float RoundFloat(float value, int decimals)
        {
            if (decimals <= 0)
                return Mathf.Round(value);

            float scale = Mathf.Pow(10f, decimals);
            return Mathf.Round(value * scale) / scale;
        }

        internal static string FormatFloat(float value, int decimals)
        {
            if (decimals <= 0)
                return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);

            string format = "0." + new string('#', decimals);
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        // --- double --------------------------------------------------------

        internal static bool TryParseDouble(string text, out double value)
        {
            value = 0d;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string normalized = text.Trim().Replace(',', '.');

            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return false;

            // "NaN"/"Infinity" parse with NumberStyles.Float, and an overflow may parse as
            // infinity on some runtimes; none of them is a number the field can show.
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                value = 0d;
                return false;
            }

            return true;
        }

        // Same rule as Mathf.Clamp (min wins when min > max).
        internal static double ClampDouble(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        // Ties to even, like the float path (Mathf.Round).
        internal static double RoundDouble(double value, int decimals)
        {
            double rounded = decimals <= 0
                ? Math.Round(value)
                : Math.Round(value, Math.Min(decimals, MaxDoubleDecimals));

            // No "-0" in the field.
            return rounded == 0d ? 0d : rounded;
        }

        internal static string FormatDouble(double value, int decimals)
        {
            if (decimals <= 0)
            {
                // Not through int: a double field may hold values beyond int's range.
                double whole = Math.Round(value);
                if (whole == 0d)
                    whole = 0d;

                return whole.ToString("0", CultureInfo.InvariantCulture);
            }

            string format = "0." + new string('#', Math.Min(decimals, MaxDoubleDecimals));
            string text = value.ToString(format, CultureInfo.InvariantCulture);

            // A tiny negative value rounds to "-0" in the custom format on some runtimes.
            return text == "-0" ? "0" : text;
        }
    }
}
