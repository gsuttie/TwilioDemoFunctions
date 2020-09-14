using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;

namespace PXDemoFunctionsLatest
{
    public static class DurableOrchestrationContextExtensions
    {
        [FunctionName("O_CallNumberAsync")]
        public static async Task<bool> CallNumberAsync(this IDurableOrchestrationContext context, string number, ILogger log, int attempts = 3)
        {
            log.LogWarning("Calling MEthod CallNumberAsync in DurableOrchestrationContextExtensions");

            var callMade_UniqueEventId = await context.CallActivityAsync<string>("A_MakeCall", number);
            bool callComplete = false;

            if (!string.IsNullOrEmpty(callMade_UniqueEventId))
            {
                var cts = new CancellationTokenSource();
                var callEvent = await context.WaitForExternalEvent<string>($"TwilioEventRaised_{callMade_UniqueEventId}", TimeSpan.FromSeconds(30), cts.Token);

                if (callEvent == null || new[] { "failed", "no-answer" }.Contains(callEvent))
                {
                    cts.Cancel();

                    if (attempts <= 0)
                        return false;

                    attempts -= 1;
                    return await context.CallNumberAsync(number, log, attempts);
                }

                return callComplete;
            }
            else 
            {
                // The call was picked up or some other happ-path state
                return callComplete;
            }
        }
    }
}
