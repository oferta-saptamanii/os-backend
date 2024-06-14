using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace best_discount
{
    public static class Utils
    {
        public static string ConvertToCurrency(string priceString)
        {
            if (string.IsNullOrEmpty(priceString) || !int.TryParse(priceString, out int priceInt))
            {
                return null;
            }

            decimal priceDecimal = priceInt / 100m;
            return priceDecimal.ToString("F2", CultureInfo.InvariantCulture);
        }

        public enum ErrorType
        {
            ERROR,
            EXCEPTION
        }

        public static void Report(
        string message,
        ErrorType type,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        {
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            string errorType = type == ErrorType.ERROR ? "ERROR" : "EXCEPTION";

            Console.WriteLine($"{className}:{lineNumber} {memberName}: {errorType} - {message}");


            // Mail / push notification the devs
            // ....
        }
    }
}
