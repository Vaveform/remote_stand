using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Text.Json;

namespace WebRTC_Remote_FPGA_stand
{
    public static class ICEExtensions
    {
        /// <summary>
        ///  Standard JSON serialization for ICE
        ///  candidate 
        /// </summary>
        public static string ToJson(this IceCandidate candidate)
        {
            return JsonSerializer.Serialize(new ICEJavaScriptNotation(candidate));
        }

        /// <summary>
        ///  JSON format serialization for
        ///  communication with Artem Baskal 
        ///  frontend
        /// </summary>
        public static string ToABJson(this IceCandidate candidate)
        {
            return "{\"data\":{\"candidate\":" + ToJson(candidate) + "}}";
        }
    }
}
