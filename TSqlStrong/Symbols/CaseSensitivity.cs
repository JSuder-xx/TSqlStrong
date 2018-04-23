using System;

namespace TSqlStrong.Symbols
{
    public enum CaseSensitivity { CaseSensitive, CaseInsensitive };

    public static class CaseSensitivityExtensions
    {
        /// <summary>
        /// Normalize the given string using this case sensitivity kind.
        /// </summary>
        /// <param name="sensitivity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string NormalizeString(this CaseSensitivity sensitivity, string value) =>
            sensitivity == CaseSensitivity.CaseSensitive
                ? value
                : value.ToUpper();
        
        public static bool AreEqual(this CaseSensitivity sensitivity, string left, string right) =>
            String.Equals(left, right, sensitivity == CaseSensitivity.CaseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);

        /// <summary>
        /// Normalize this string given the case sensitivity kind.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="sensitivity"></param>
        /// <returns></returns>
        public static string ToCaseSensitivityNormalizedString(this string value, CaseSensitivity sensitivity) => sensitivity.NormalizeString(value);
    }
}
