using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TeleradiologyCore.Extensions
{
    /// <summary>
    /// Extension methods for String class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Adds a char to end of given string if it does not ends with the char.
        /// </summary>
        public static string EnsureEndsWith(this string str, char c)
        {
            return EnsureEndsWith(str, c, StringComparison.Ordinal);
        }

        /// <summary>
        /// Adds a char to end of given string if it does not ends with the char.
        /// </summary>
        public static string EnsureEndsWith(this string str, char c, StringComparison comparisonType)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.EndsWith(c.ToString(), comparisonType))
            {
                return str;
            }

            return str + c;
        }

        /// <summary>
        /// Adds a char to end of given string if it does not ends with the char.
        /// </summary>
        public static string EnsureEndsWith(this string str, char c, bool ignoreCase, CultureInfo culture)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.EndsWith(c.ToString(culture), ignoreCase, culture))
            {
                return str;
            }

            return str + c;
        }

        /// <summary>
        /// Adds a char to beginning of given string if it does not starts with the char.
        /// </summary>
        public static string EnsureStartsWith(this string str, char c)
        {
            return EnsureStartsWith(str, c, StringComparison.Ordinal);
        }

        /// <summary>
        /// Adds a char to beginning of given string if it does not starts with the char.
        /// </summary>
        public static string EnsureStartsWith(this string str, char c, StringComparison comparisonType)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.StartsWith(c.ToString(), comparisonType))
            {
                return str;
            }

            return c + str;
        }

        /// <summary>
        /// Adds a char to beginning of given string if it does not starts with the char.
        /// </summary>
        public static string EnsureStartsWith(this string str, char c, bool ignoreCase, CultureInfo culture)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.StartsWith(c.ToString(culture), ignoreCase, culture))
            {
                return str;
            }

            return c + str;
        }

        /// <summary>
        /// Indicates whether this string is null or an System.String.Empty string.
        /// </summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// indicates whether this string is null, empty, or consists only of white-space characters.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
        public static string Left(this string str, int len)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length < len)
            {
                throw new ArgumentException("len argument can not be bigger than given string's length!");
            }

            return str.Substring(0, len);
        }

        /// <summary>
        /// Converts line endings in the string to <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string NormalizeLineEndings(this string str)
        {
            return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// Gets index of nth occurence of a char in a string.
        /// </summary>
        /// <param name="str">source string to be searched</param>
        /// <param name="c">Char to search in <see cref="str"/></param>
        /// <param name="n">Count of the occurence</param>
        public static int NthIndexOf(this string str, char c, int n)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            var count = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] != c)
                {
                    continue;
                }

                if ((++count) == n)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes first occurrence of the given postfixes from end of the given string.
        /// Ordering is important. If one of the postFixes is matched, others will not be tested.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="postFixes">one or more postfix.</param>
        /// <returns>Modified string or the same string if it has not any of given postfixes</returns>
        public static string RemovePostFix(this string str, params string[] postFixes)
        {
            if (str == null)
            {
                return null;
            }

            if (str == string.Empty)
            {
                return string.Empty;
            }

            if (postFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (var postFix in postFixes)
            {
                if (str.EndsWith(postFix))
                {
                    return str.Left(str.Length - postFix.Length);
                }
            }

            return str;
        }

        /// <summary>
        /// Removes first occurrence of the given prefixes from beginning of the given string.
        /// Ordering is important. If one of the preFixes is matched, others will not be tested.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="preFixes">one or more prefix.</param>
        /// <returns>Modified string or the same string if it has not any of given prefixes</returns>
        public static string RemovePreFix(this string str, params string[] preFixes)
        {
            if (str == null)
            {
                return null;
            }

            if (str == string.Empty)
            {
                return string.Empty;
            }

            if (preFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (var preFix in preFixes)
            {
                if (str.StartsWith(preFix))
                {
                    return str.Right(str.Length - preFix.Length);
                }
            }

            return str;
        }

        /// <summary>
        /// Gets a substring of a string from end of the string.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
        public static string Right(this string str, int len)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length < len)
            {
                throw new ArgumentException("len argument can not be bigger than given string's length!");
            }

            return str.Substring(str.Length - len, len);
        }

        /// <summary>
        /// Uses string.Split method to split given string by given separator.
        /// </summary>
        public static string[] Split(this string str, string separator)
        {
            return str.Split(new[] { separator }, StringSplitOptions.None);
        }

        /// <summary>
        /// Uses string.Split method to split given string by given separator.
        /// </summary>
        public static string[] Split(this string str, string separator, StringSplitOptions options)
        {
            return str.Split(new[] { separator }, options);
        }

        /// <summary>
        /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string[] SplitToLines(this string str)
        {
            return str.Split(Environment.NewLine);
        }

        /// <summary>
        /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string[] SplitToLines(this string str, StringSplitOptions options)
        {
            return str.Split(Environment.NewLine, options);
        }

        /// <summary>
        /// Converts PascalCase string to camelCase string.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="invariantCulture">Invariant culture</param>
        /// <returns>camelCase of the string</returns>
        public static string ToCamelCase(this string str, bool invariantCulture = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return invariantCulture ? str.ToLowerInvariant() : str.ToLower();
            }

            return (invariantCulture ? char.ToLowerInvariant(str[0]) : char.ToLower(str[0])) + str.Substring(1);
        }

        /// <summary>
        /// Converts PascalCase string to camelCase string in specified culture.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="culture">An object that supplies culture-specific casing rules</param>
        /// <returns>camelCase of the string</returns>
        public static string ToCamelCase(this string str, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return str.ToLower(culture);
            }

            return char.ToLower(str[0], culture) + str.Substring(1);
        }

        /// <summary>
        /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
        /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <param name="invariantCulture">Invariant culture</param>
        public static string ToSentenceCase(this string str, bool invariantCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return Regex.Replace(
                str,
                "[a-z][A-Z]",
                m => m.Value[0] + " " + (invariantCulture ? char.ToLowerInvariant(m.Value[1]) : char.ToLower(m.Value[1]))
            );
        }

        /// <summary>
        /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
        /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        public static string ToSentenceCase(this string str, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1], culture));
        }

        /// <summary>
        /// Converts string to enum value.
        /// </summary>
        /// <typeparam name="T">Type of enum</typeparam>
        /// <param name="value">String value to convert</param>
        /// <returns>Returns enum object</returns>
        public static T ToEnum<T>(this string value)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (T)Enum.Parse(typeof(T), value);
        }

        /// <summary>
        /// Converts string to enum value.
        /// </summary>
        /// <typeparam name="T">Type of enum</typeparam>
        /// <param name="value">String value to convert</param>
        /// <param name="ignoreCase">Ignore case</param>
        /// <returns>Returns enum object</returns>
        public static T ToEnum<T>(this string value, bool ignoreCase)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        public static string ToMd5(this string str)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(str);
                var hashBytes = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                foreach (var hashByte in hashBytes)
                {
                    sb.Append(hashByte.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Converts camelCase string to PascalCase string.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="invariantCulture">Invariant culture</param>
        /// <returns>PascalCase of the string</returns>
        public static string ToPascalCase(this string str, bool invariantCulture = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return invariantCulture ? str.ToUpperInvariant() : str.ToUpper();
            }

            return (invariantCulture ? char.ToUpperInvariant(str[0]) : char.ToUpper(str[0])) + str.Substring(1);
        }

        /// <summary>
        /// Converts camelCase string to PascalCase string in specified culture.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="culture">An object that supplies culture-specific casing rules</param>
        /// <returns>PascalCase of the string</returns>
        public static string ToPascalCase(this string str, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return str.ToUpper(culture);
            }

            return char.ToUpper(str[0], culture) + str.Substring(1);
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string Truncate(this string str, int maxLength)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            return str.Left(maxLength);
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
        /// It adds a "..." postfix to end of the string if it's truncated.
        /// Returning string can not be longer than maxLength.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string TruncateWithPostfix(this string str, int maxLength)
        {
            return TruncateWithPostfix(str, maxLength, "...");
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
        /// It adds given <paramref name="postfix"/> to end of the string if it's truncated.
        /// Returning string can not be longer than maxLength.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string TruncateWithPostfix(this string str, int maxLength, string postfix)
        {
            if (str == null)
            {
                return null;
            }

            if (str == string.Empty || maxLength == 0)
            {
                return string.Empty;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            if (maxLength <= postfix.Length)
            {
                return postfix.Left(maxLength);
            }

            return str.Left(maxLength - postfix.Length) + postfix;
        }

        public static string ToBase64(this string value)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(valueBytes);
        }

        public static string Base64ToString(this string value)
        {
            var valueBytes = System.Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(valueBytes);
        }

        public static string ToHex(this byte[] the_bytes, char separator = '-')
        {
            return BitConverter.ToString(
                the_bytes, 0).Replace('-', separator);
        }
        public static DateTime? ToDateTime(this string inputString)
        {
            try
            {
                var dateString = Regex.Replace(inputString, "(th|st|rd|,|'|\\|\")", " ", RegexOptions.IgnoreCase);
                Regex regex = new Regex(@"[ ]{2,}", RegexOptions.None); //replace 1+ space into one
                dateString = regex.Replace(dateString, " ");

                #region Formats
                var formats = new[] {
                "M/dd/yy h:m:s tt",
                "M/d/yy h:m:s tt",
                "MM/d/yy h:m:s tt",
                "MM/dd/yy hh:mm:ss tt",
                "MM/dd/yy hh:mm:ss tt",
                "MM/dd/yy HH:mm:ss",
                "MM/dd/yy HH:mm:ss tt",
                "MM/dd/yy h:mm:ss tt",
                "M/d/yyyy h:mm:ss tt",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy hh:mm:ss tt",

                "yyyy-MM-dd",
                "yyyy.MM.dd",
                "yyyy MM dd",
                "yyyy MMM d",
                "yyyy MMM dd",

                "yyyyMMdd",
                "d/M/yyyy",
                "dd/M/yyyy",
                "d/MM/yyyy",
                "dd/MM/yyyy",
                "d/M/yy",
                "dd/M/yy",
                "d/MM/yy",
                "dd/MM/yy",

                "d-M-yyyy",
                "dd-M-yyyy",
                "d-MM-yyyy",
                "dd-MM-yyyy",
                "d-M-yy",
                "dd-M-yy",
                "d-MM-yy",
                "dd-MM-yy",

                "d.M.yyyy",
                "dd.M.yyyy",
                "d.MM.yyyy",
                "dd.MM.yyyy",
                "d.M.yy",
                "dd.M.yy",
                "d.MM.yy",
                "dd.MM.yy",
                "d M yyyy",
                "dd M yyyy",
                "d MM yyyy",
                "dd MM yyyy",
                "d M yy",
                "dd M yy",
                "d MM yy",
                "dd MM yy",
                "d MMM yy",
                "d MMMM yy",
                "dd MMM yyyy",
                "dd MMMM yyyy",
                "dd MMM yyyy",
                "dMMMyy",
                "ddMMMyy",
                "dMMMyyyy",
                "ddMMMyyyy",
                "dMMMMyy",
                "ddMMMMyy",
                "dMMMMyyyy",
                "ddMMMMyyyy",

                "d MMMyy",
                "dd MMMyy",
                "d MMMyyyy",
                "dd MMMyyyy",
                "d MMMMyy",
                "dd MMMMyy",
                "d MMMMyyyy",
                "dd MMMMyyyy"

            };
                #endregion

                var dateValue = DateTime.ParseExact(
                    dateString, formats, System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None);

                return dateValue;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string ToQueryOperator(this string strOp)
        {
            strOp = strOp.IsNullOrEmpty() ? string.Empty : strOp;
            string op = string.Empty;
            switch (strOp.ToLower())
            {
                case "eq":
                case "=":
                    op = "==";
                    break;
                case "gt":
                case ">":
                    op = ">";
                    break;
                case "lt":
                case "<":
                    op = "<";
                    break;
                case "ge":
                case ">=":
                    op = ">=";
                    break;
                case "le":
                case "<=":
                    op = "<=";
                    break;
                case "ne":
                case "!=":
                    op = "!=";
                    break;
                case "cw":
                case "contains":
                case "like":
                    op = "Contains";
                    break;
                case "in":
                    op = "Contains[]";
                    break;
                case "sw":
                case "startswith":
                    op = "StartsWith";
                    break;
                case "ew":
                case "endswith":
                    op = "EndsWith";
                    break;
                default:
                    op = "==";
                    break;
            }
            return op;
        }
    }
}
