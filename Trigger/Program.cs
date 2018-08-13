using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
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
                    Console.WriteLine(" -> Queuing message");
                    QueueMessage();
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

            var messageContent = DateTime.Now.Ticks.ToString();
            var message = new CloudQueueMessage(messageContent);
            queue.AddMessage(message);
        }
    }
}
