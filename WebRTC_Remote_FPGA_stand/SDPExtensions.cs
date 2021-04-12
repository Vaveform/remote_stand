/* 
 * This extensions methods provide serialization format
 * for communication with client browser, which 
 * send/receive SDP offers and answers for establish 
 * WebRTC connection
*/
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Web;

namespace WebRTC_Remote_FPGA_stand
{
    public static class SDPExtensions
    {
        /// <summary>
        ///  Standard JSON serialization for session
        ///  description protocol 
        /// </summary>
        public static string ToJson(this SdpMessage SDP) {
            return "{\"type\":\"" + SdpMessage.TypeToString(SDP.Type) + 
                "\",\"sdp\":\"" + HttpUtility.JavaScriptStringEncode(SDP.Content) + 
                "\"}";
        }

        /// <summary>
        ///  JSON format serialization for
        ///  communication with Artem Baskal 
        ///  frontend
        /// </summary>
        public static string ToABJson(this SdpMessage SDP) {
            return "{\"data\":{\"description\":" + ToJson(SDP) + "}}";
        }
    }
}
