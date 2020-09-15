using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using PXDemoFunctions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PXDemoFunctionsLatest
{

    //// https://github.com/Azure/azure-functions-durable-extension/blob/dev/samples/precompiled/PhoneVerification.cs

    public static class DurableOrchestrationContextExtensions
    {
        [FunctionName("O_CallNumberAsync")]
        public static async Task<bool> CallNumberAsync(this IDurableOrchestrationContext context, string number, ILogger log, int attempts = 3)
        {
            log.LogWarning("Calling Method CallNumberAsync in DurableOrchestrationContextExtensions");

            CallInfo callinfo = new CallInfo
            {
                NumberCalled = number
            };

            var callMade_UniqueEventId = await context.CallActivityAsync<string>("A_MakeCall", callinfo);

            log.LogWarning($"callMade_UniqueEventId = {callMade_UniqueEventId}");

            if (!string.IsNullOrEmpty(callMade_UniqueEventId))
            {
                var cts = new CancellationTokenSource();
                var callEvent = await context.WaitForExternalEvent<string>($"TwilioEventRaised_{callMade_UniqueEventId}", TimeSpan.FromSeconds(20), cts.Token);

                if (callEvent == null || new[] { "failed", "busy", "no-answer" }.Contains(callEvent))
                {
                    cts.Cancel();

                    if (attempts <= 0)
                        return false;

                    attempts -= 1;
                    return await context.CallNumberAsync(number, log, attempts);
                }

                return false;
            }
            else 
            {
                // The call was picked up or some other happ-path state
                return true;
            }
        }
    }
}