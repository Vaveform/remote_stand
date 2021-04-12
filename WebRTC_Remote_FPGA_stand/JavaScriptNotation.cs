using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.MixedReality.WebRTC;

namespace WebRTC_Remote_FPGA_stand
{
    public interface IConvertToMRWebRTCNotation<T>
    {
        public T ToMRNetCoreNotation();
    }
    public class ICEJavaScriptNotation : IConvertToMRWebRTCNotation<IceCandidate>
    {

        public ICEJavaScriptNotation(IceCandidate ice) {
            candidate = ice.Content;
            sdpMid = ice.SdpMid;
            sdpMLineIndex = ice.SdpMlineIndex;
        }
        public ICEJavaScriptNotation() { }
        public IceCandidate ToMRNetCoreNotation () {
            return new IceCandidate { Content = candidate, SdpMid = sdpMid, SdpMlineIndex = sdpMLineIndex};
        }
        public string candidate { get; set; }
        public string sdpMid { get; set; }
        public int sdpMLineIndex { get; set; }
    }
    public class SDPJavaScriptNotation : IConvertToMRWebRTCNotation<SdpMessage>
    {
        public SDPJavaScriptNotation(SdpMessage session_description)
        {
            type = SdpMessage.TypeToString(session_description.Type);
            sdp = session_description.Content;
        }
        public SDPJavaScriptNotation() { }
        public SdpMessage ToMRNetCoreNotation()
        {
            return new SdpMessage { Type = SdpMessage.StringToType(type), Content = sdp };
        }
        public string type { get; set; }
        public string sdp { get; set; }
    }
}
