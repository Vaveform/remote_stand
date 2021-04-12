using System;
using System.Collections.Generic;
using System.Text;

namespace WebRTC_Remote_FPGA_stand
{
    public static class StringExtensions
    {
        public static (string, string) DivideHeaderAndOriginalJSON(this string JsonStr)
        {
            int index = JsonStr.IndexOf(':', JsonStr.IndexOf(':') + 1);
            if (index == -1)
            {
                return ("", JsonStr);
            }
            return (JsonStr.Substring(0, index + 1), JsonStr.Substring(index + 1, JsonStr.Length - index - 3));
        }
    }
}
