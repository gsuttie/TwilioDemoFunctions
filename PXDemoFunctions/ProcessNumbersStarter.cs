using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;

namespace PXDemoFunctions
{
    public static class ProcessNumbersStarter
    {
        [FunctionName("ProcessNumbersStarter")]
        public static async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
           [DurableClient] IDurableOrchestrationClient starter,
           ILogger log)
        {

            string number = req.Query["number"];

            // parse query parameter 
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            number = number ?? data?.number;

            if (number == null)
            {
                string responseMessage = string.IsNullOrEmpty(number)
                ? "This HTTP triggered function executed successfully. Pass a number in the query string or in the request body for a personalized response."
                : $"Hello, {number}. This HTTP triggered function executed successfully.";
            }

            log.LogInformation($"About to start orchestration for {number}");

            var orchestrationId = await starter.StartNewAsync("O_ProcessNumbers", null, number);

            return starter.CreateCheckStatusResponse(req, orchestrationId);
        }
    }
}
