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
            Console.WriteLine("Press Q to Quitm or:");
            Console.WriteLine("1 to push a new mail send request into the queue: English with black list check");
            Console.WriteLine("2 to push a new mail send request into the queue: English without black list check");
            Console.WriteLine("3 to push a new mail send request into the queue: Dutch with black list check");
            Console.WriteLine("4 to push a new mail send request into the queue: Dutch without black list check");

            var c = Console.ReadKey();
            while (c.Key != ConsoleKey.Q)
            {
                if (c.KeyChar == '1' || c.KeyChar == '2' || c.KeyChar == '3' || c.KeyChar == '4')
                {
                    int lcid = (c.KeyChar == '1' || c.KeyChar == '2') ? 1033 : 1043;
                    bool checkBlacklist = (c.KeyChar == '1' || c.KeyChar == '3') ? true : false;

                    Console.Write($" -> Queuing message ({lcid},{checkBlacklist})... ");
                    QueueMessage(lcid, checkBlacklist);
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

        private static void QueueMessage(int lcid, bool checkBlacklist)
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("QueueName"));
            queue.CreateIfNotExists();

            var message = new MailSender.Message()
            {
                RecipientAddress = "abc@def.gh",
                CheckBlacklist = checkBlacklist,
                Lcid = lcid,
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
