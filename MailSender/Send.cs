using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace MailSender
{
    public static class Send
    {
        [FunctionName("Send")]
        public static void Run([QueueTrigger("mailqueue", Connection = "AzureWebJobsStorage")]string queueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {queueItem}");
        }
    }
}
