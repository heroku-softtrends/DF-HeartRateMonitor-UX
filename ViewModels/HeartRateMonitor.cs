using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DreamforceIOTCloudApp.ViewModels
{
    public class HeartRateMonitor
    {
        public string deviceID { get; set; }
        public int heartRate { get; set; }
        public int goalDuration { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
    }
}
