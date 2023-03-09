using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Exaroton.Cast
{
    // taken from https://github.com/takunology/MinecraftConnection.git
    // thanks them a lot!
    // (already modified most of their work, but still thanks to them!)

    internal static class Cast
    {
        internal static T ConvertData<T>(this string response)
        {
            if(response == "") return default;
            //Console.WriteLine(response);
            var type = typeof(T);
            var args = response.Split("entity data: ");
            if(args.Length < 2 || args[1] == "" || args[1] is null) throw new Exception();

            response = args[1];

            if(type != typeof(string))
            {
                response = response.Replace("d", "");
                response = response.Replace("s", "");
                response = response.Replace("f", "");
                response = response.Replace("b", "");
                response = response.Replace(".", ",");
            }

            if(type == typeof(double)) return (T)((object)response.DataToDouble());
            if(type == typeof(float)) return (T)((object)response.DataToFloat());
            if(type == typeof(int)) return (T)((object)response.DataToInt());
            if(type == typeof(short)) return (T)((object)response.DataToShort());
            if(type == typeof(bool)) return (T)((object)response.DataToBool());
            if(type == typeof(string)) return (T)((object)response.DataToString());

            throw new NotImplementedException();
        }

        internal static double DataToDouble(this string response)
        {
            return Convert.ToDouble(response);
        }
        
        internal static double DataToFloat(this string response)
        {
            return Convert.ToSingle(response);
        }

        internal static int DataToInt(this string response)
        {
            return Convert.ToInt32(response);
        }

        internal static short DataToShort(this string response)
        {
            return Convert.ToInt16(response);
        }

        internal static bool DataToBool(this string response)
        {
            return response == "1";
        }

        internal static string DataToString(this string response)
        {
            return response;
        }
    }
}