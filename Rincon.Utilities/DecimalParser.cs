using System.Globalization;

namespace Rincon.Utilities
{
    public static class DecimalParser
    {
        public static bool TryParse(string? value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim()
                .Replace("$", "")
                .Replace(" ", "");

            bool hasComma = value.Contains(",");
            bool hasDot = value.Contains(".");

            if (hasComma && hasDot)
            {
                int lastComma = value.LastIndexOf(",");
                int lastDot = value.LastIndexOf(".");

                value = lastComma > lastDot
                    ? value.Replace(".", "").Replace(",", ".")
                    : value.Replace(",", "");
            }
            else if (hasComma)
            {
                int commaCount = value.Split(',').Length - 1;
                int lastComma = value.LastIndexOf(",");
                int digitsAfterComma = value.Length - lastComma - 1;

                value = commaCount == 1 && digitsAfterComma == 3
                    ? value.Replace(",", "")
                    : value.Replace(",", ".");
            }
            else if (hasDot)
            {
                int dotCount = value.Split('.').Length - 1;
                int lastDot = value.LastIndexOf(".");
                int digitsAfterDot = value.Length - lastDot - 1;

                if (dotCount == 1 && digitsAfterDot == 3)
                {
                    value = value.Replace(".", "");
                }
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}
