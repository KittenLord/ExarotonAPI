using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Exaroton.Cast
{
    // taken from https://github.com/takunology/MinecraftConnection.git
    // thanks them a lot!

    internal static class Cast
    {
        internal static double DataToDouble(this string response)
        {
            string result = response.Substring(response.IndexOf("data"));
            result = Regex.Replace(result, @"[^0-9-,.]", "");
            return double.Parse(result);
        }

        internal static int DataToInt(this string response)
        {
            string result = response.Substring(response.IndexOf("data"));
            result = Regex.Replace(result, @"[^0-9-,.]", "");
            return int.Parse(result);
        }

        internal static short DataToShort(this string response)
        {
            string result = response.Substring(response.IndexOf("data"));
            result = Regex.Replace(result, @"[^0-9-,.]", "");
            return short.Parse(result);
        }

        internal static bool DataToBool(this string response)
        {
            string result = response.Substring(response.IndexOf("data"));
            result = Regex.Replace(result, @"[^0-9-,.]", "");
            if (result is "1") return true;
            else return false;
        }

        internal static string DataToString(this string response)
        {
            string result = response.Substring(response.IndexOf("data"));
            result = Regex.Replace(result, @"[^0-9-,.]", "");
            return result;
        }
    }
}