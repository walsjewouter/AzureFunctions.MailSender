using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trigger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press Q to Quit and S to push a new mail send request into the queue.");
            var c = Console.ReadKey();
            while (c.Key != ConsoleKey.Q)
            {
                if (c.Key == ConsoleKey.S)
                {
                    Console.Write(" -> Queuing message... ");
                    QueueMessage();
                    Console.WriteLine("Queued");
                }
                else
                {
                    Console.WriteLine(" -> Invalid key pressed");
                }

                c = Console.ReadKey();
            }

            Console.WriteLine("\r\n\r\nQuitting\r\n");
        }

        private static void QueueMessage()
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("QueueName"));
            queue.CreateIfNotExists();

            var message = new MailSender.Message()
            {
                RecipientAddress = "csource@live.nl",
                CheckBlacklist = true,
                Lcid = 1033,
                TemplateType = "CSource.Kayakers.Common.Enums.MailTemplateType:CPT_ReinviteForOfficial",
            };

            var model = new MailModel()
            {
                TournamentName = "Your tournament name",
                FirstName = "invited.user@email.address",
                InvitingOfficalName = "UsersFirstName",
                CallbackUrl = "https://some.url.to.a/web-page",
                RecipientAddress = message.RecipientAddress
            };

            message.SetModel(model);

            var queueMessage = new CloudQueueMessage(message.ToJsonString());
            queue.AddMessage(queueMessage);
        }
    }
}
