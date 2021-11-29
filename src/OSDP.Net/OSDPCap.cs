// ReSharper disable InconsistentNaming
namespace OSDP.Net
{
    internal struct OSDPCap
    {
        public string timeSec { get; set; }

        public string timeNano { get; set; }

        public string io { get; set; }

        public string data { get; set; }

        public string osdpTraceVersion => "1";

        public string osdpSource => "OSDP.Net";
    }
}