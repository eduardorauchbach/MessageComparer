using System.Collections.Generic;

namespace MessageComparer.Engine.Models
{
    public class SetupData
    {
        public List<KeysConfigData> KeysConfigurations { get; set; }

        public string Message1 { get; set; }
        public string Message2 { get; set; }

        public string MessageTitle1 { get; set; }
        public string MessageTitle2 { get; set; }
    }
}
