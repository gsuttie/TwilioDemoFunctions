using System;
using System.Collections.Generic;
using System.Text;

namespace PXDemoFunctions
{
    public class CallInfo
    {
         public string Sid { get; set; }
        public string Error { get; set; }

        public string Message { get; set; }

        public string[] Numbers { get; set; } // e.g. +440123456, +4498765432

        public string InstanceId { get; set; }
    }
}
