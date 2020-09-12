using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace PXDemoFunctions
{
    public static class ProcessNumbersOrchestrators
    {
        [FunctionName("O_ProcessNumbers")]
        public static async Task<object> ProcessNumbers([OrchestrationTrigger] IDurableOrchestrationContext ctx, ILogger log)
        {
            string[] numbers = null;
            string callinfoAfterCall = null;
            string answeredCallTaskResult = null;

            // ***************************** STEP 1 - get the phone numbers from storage* *****************************************

            if (!ctx.IsReplaying) 
            { 
                log.LogWarning("About to call A_GetNumbersFromStorage activity");
            }

            // Get the number from storage
            numbers = await ctx.CallActivityAsync<string[]>("A_GetNumbersFromStorage", null);

            CallInfo callinfo = new CallInfo
            {
                InstanceId = ctx.InstanceId,
                Numbers = numbers
            };

            // ***************************** STEP 2 - Attempt to make a call ******************************************
            if (!ctx.IsReplaying)
            {
                log.LogWarning("About to call A_MakeCall activity");
            }

            callinfoAfterCall = await ctx.CallActivityAsync<string>("A_MakeCall", callinfo);

            using (var cts = new CancellationTokenSource())
            {
                var timeout = ctx.CurrentUtcDateTime.AddSeconds(60);
                var timeoutTask = ctx.CreateTimer(timeout, cts.Token);
                var answeredCallTask = ctx.WaitForExternalEvent<string>("AnsweredCallResult", TimeSpan.FromSeconds(30), cts.Token);

                var winner = await Task.WhenAny(answeredCallTask, timeoutTask);
                if (winner == answeredCallTask)
                {
                    log.LogWarning($"Call answered at {ctx.CurrentUtcDateTime}");

                    answeredCallTaskResult = answeredCallTask.Result;
                    cts.Cancel();
                }
                else
                {
                    answeredCallTaskResult = "Timed Out";
                }
            }

            return new
            {
                blah = "blah"
            };
        }
    }
}
