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
            Console.WriteLine("Press Q to Quit or:");
            Console.WriteLine("1 to push a new mail send request into the queue: English with black list check");
            Console.WriteLine("2 to push a new mail send request into the queue: English without black list check");
            Console.WriteLine("3 to push a new mail send request into the queue: Dutch with black list check");
            Console.WriteLine("4 to push a new mail send request into the queue: Dutch without black list check");
            Console.WriteLine("5 to push a new mail send request into the queue: German with black list check");
            Console.WriteLine("6 to push a new mail send request into the queue: German without black list check");

            var c = Console.ReadKey();
            while (c.Key != ConsoleKey.Q)
            {
                int lcid;
                switch (c.KeyChar)
                {
                    case '1':
                    case '2':
                        lcid = 1033;
                        break;

                    case '3':
                    case '4':
                        lcid = 1043;
                        break;

                    case '5':
                    case '6':
                        lcid = 1031;
                        break;

                    default:
                        lcid = 0;
                        break;
                }

                if (lcid == 0)
                {
                    Console.WriteLine(" -> Invalid key pressed");
                    continue;
                }

                bool checkBlacklist = c.KeyChar == '1' || c.KeyChar == '3' || c.KeyChar == '5';

                Console.Write($" -> Queuing message ({lcid},{checkBlacklist})... ");
                QueueMessage(lcid, checkBlacklist);
                Console.WriteLine("Queued");

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
