using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace PXDemoFunctions
{
    public static class ProcessNumbersActivities
    {
        [FunctionName("A_GetNumbersFromStorage")]
        public static async Task<string[]> GetNumbersFromStorage([ActivityTrigger] string inputNumbers, ILogger log)
        {
            log.LogWarning($"GetNumbersFromStorage {inputNumbers}");

            string connectionString = Environment.GetEnvironmentVariable("funcdemostorConnString");

            string containerName = Environment.GetEnvironmentVariable("NumbersContainerName");
            string fileName = Environment.GetEnvironmentVariable("NumbersFileName");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Connect to the blob storage
            CloudBlobClient serviceClient = storageAccount.CreateCloudBlobClient();

            // Connect to the blob container
            CloudBlobContainer container = serviceClient.GetContainerReference($"{containerName}");

            // Connect to the blob file
            CloudBlockBlob blob = container.GetBlockBlobReference($"{fileName}");

            // Get the blob file as text
            string contents = blob.DownloadTextAsync().Result;

            string[] numbers = contents.Split(',');

            await Task.Delay(100);

            return numbers;
        }

        [FunctionName("A_MakeCall")]
        public static async Task<string> MakeCall([ActivityTrigger] CallInfo callInfo, ILogger log)
        {
            log.LogWarning($"MakeCall {callInfo.Numbers}");

            string accountSid = Environment.GetEnvironmentVariable("accountSid");
            string authToken = Environment.GetEnvironmentVariable("authToken");
            TwilioClient.Init(accountSid, authToken);

            var myfileURI = new Uri("https://github.com/gsuttie/TwilioAzurefunctions/blob/master/message.xml");

            // ***********************************************************************************************************
            //TODO figure out how best to call this and loop thru the numbers instead of hardcoding the first number below
            // ***********************************************************************************************************

            var to = new PhoneNumber(callInfo.Numbers[0]);
            var from = new PhoneNumber(Environment.GetEnvironmentVariable("twilioDemoNumber"));

            log.LogWarning($"InstanceId {callInfo.InstanceId}");

            var statusCallbackUri = new Uri(string.Format(Environment.GetEnvironmentVariable("statusCallBackUrl"), callInfo.InstanceId));

            log.LogWarning($"statusCallbackUri {statusCallbackUri}");

            var call = CallResource.Create(
                to,
                from,
                url: new Uri("http://demo.twilio.com/docs/voice.xml"),
                statusCallback: statusCallbackUri,
                statusCallbackMethod: Twilio.Http.HttpMethod.Post);

            await Task.Delay(100);

            CallInfo myCallinfo = new CallInfo
            {
                Sid = call.Sid
            };

            //return myCallinfo;
            return "MakeCall completed";
        }
    }
}