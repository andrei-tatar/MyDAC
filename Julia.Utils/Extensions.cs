using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Julia.Utils
{
    public static class Extensions
    {
        public static string[] GetLines(this string input)
        {
            return input.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public static void ForEach<T>(this IEnumerable<T> input, Action<T> action)
        {
            foreach (var v in input)
                action(v);
        }

        public static ConsoleResult CheckForExceptionOrError(this ConsoleResult result, string messageToThrow)
        {
            if (result.Exception != null)
                throw new Exception(messageToThrow + "(" + result.Exception.Message + ")");
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new Exception(messageToThrow + "(" + result.Error + ")");
            return result;
        }

        public static string KeepNumerals(this string text, int maxLength = int.MaxValue)
        {
            var result = "";
            foreach (var ch in text)
            {
                if (ch >= '0' && ch <= '9')
                    result += ch;
                if (result.Length >= maxLength)
                    break;
            }
            return result;
        }

        public static string FormatWith(this string text, params object[] args)
        {
            return string.Format(text, args);
        }
    }
}
