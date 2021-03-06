﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace PXDemoFunctions
{
    public static class ProcessNumbersOrchestrators
    {
        [FunctionName("O_ProcessNumbers")]
        public static async Task<object> ProcessNumbers([OrchestrationTrigger] 
        IDurableOrchestrationContext ctx, ILogger log)
        {
            string[] numbers = null;
            string callinfoAfterCall = null;
            string answeredCallTaskResult = null;
            bool callAnswered = false;
            
            try 
            {
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


                // Attempt to call the numbers retrieved from storage
                ////foreach (var number in numbers)
                ////{
                ////    var done = false;
                ////    done = await ctx.CallNumberAsync(number, log);

                ////    if (done)
                ////    {
                ////        break;
                ////    }
                ////}

                callinfoAfterCall = await ctx.CallActivityAsync<string>("A_MakeCall", callinfo);

                using (var cts = new CancellationTokenSource())
                {
                    var timeout = ctx.CurrentUtcDateTime.AddSeconds(60);
                    var timeoutTask = ctx.CreateTimer(timeout, cts.Token);
                    var answeredCallTask = ctx.WaitForExternalEvent<string>("AnsweredCallResult", TimeSpan.FromSeconds(60), cts.Token);

                    var winner = await Task.WhenAny(answeredCallTask, timeoutTask);
                    if (winner == answeredCallTask)
                    {
                        log.LogWarning($"Call answered at {ctx.CurrentUtcDateTime}");
                        callAnswered = true;

                        answeredCallTaskResult = answeredCallTask.Result;
                        cts.Cancel();
                    }
                    else
                    {
                        callAnswered = false;
                        answeredCallTaskResult = "Timed Out";
                    }
                }

                return new
                {
                    CallAnswered = callAnswered,
                    Success = true,
                    OrchestrationId = ctx.InstanceId
                };
            }
            catch (Exception e)
            {
                // Log Exception, pefrom any cleanup
                return new
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }
    }
}