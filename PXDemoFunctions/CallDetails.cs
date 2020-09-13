using System;
using System.Collections.Generic;
using System.Text;

namespace PXDemoFunctionsLatest
{
    public class CallDetails
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string OrchestrationId { get; set; }

        public string NumberCalled { get; set; }
    }
}
