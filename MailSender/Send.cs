using System;
using System.Configuration;
using Azure.Data.Tables;
using MailSender.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

namespace MailSender
{
    public class Send
    {
        private readonly TableServiceClient? _serviceClient = null;

        protected TableServiceClient ServiceClient => _serviceClient ?? new TableServiceClient(GetEnvironmentVariable("StorageConnection"));

        [FunctionName("Send")]
        public void Run([QueueTrigger("mailqueue", Connection = "AzureWebJobsStorage")] string queueItem, ILogger log)
        {
            log.LogInformation("Queue trigger function processing queue item");

            log.LogDebug("Restoring queue message object");
            var message = Message.FromJsonString(queueItem);
            var model = message.GetModel();
            var modelType = model.GetType();

            bool doSend = true;
            if (message.CheckBlacklist)
            {
                if (IsRecipientBlackListed(message.RecipientAddress, log))
                {
                    doSend = false;
                }
            }

            if (doSend)
            {
                // Get the mail template from table storage
                var template = GetMailTemplate(message, log);

                string subject = template.Subject;
                string salution = template.Salution;
                string body = template.Body;

                // Get the correct base e-mail html
                var html = GetHtmlTemplate(message, log);
                // Replace tokens in HTML
                log.LogDebug("Replacing tokens in html");
                html = html.Replace("[[%SALUTION%]]", salution);
                html = html.Replace("[[%BODY%]]", body);
                html = html.Replace("[[%BLACKLISTURL%]]", message.BlackListUrl);

                log.LogInformation("Creating mail message");
                using var mail = new MailMessage();
                mail.From = new MailAddress(GetEnvironmentVariable("SenderAddress"), GetEnvironmentVariable("SenderName"));
                mail.To.Add(new MailAddress(message.RecipientAddress));
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.IsBodyHtml = true;
                mail.Subject = subject;
                mail.Body = html;

                log.LogDebug("Instantiating SMTP client");
                int mailPort = int.Parse(GetEnvironmentVariable("MailPort"));
                using var smtpClient = new SmtpClient(GetEnvironmentVariable("MailHost"), mailPort);

                string mailServerUsername = GetEnvironmentVariable("MailServerUsername");
                if (!string.IsNullOrWhiteSpace(mailServerUsername))
                {
                    smtpClient.Credentials = new NetworkCredential(mailServerUsername, GetEnvironmentVariable("MailServerPassword"));
                }

                if (smtpClient.Host == "localhost")
                {
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    smtpClient.PickupDirectoryLocation = "D:\\Temp\\Mail";
                }

                log.LogInformation("Sending mail");
                smtpClient.Send(mail);
            }
            else
            {
                log.LogWarning("Address is black listed, NOT sending mail");
            }

            log.LogInformation("Queue trigger function processed queue item");

        }

        private MailTemplate GetMailTemplate(Message message, ILogger log)
        {
            log.LogInformation($"Retrieving mail text parts for template type {message.TemplateType} for language {message.Lcid}");

            string tableName = GetEnvironmentVariable("MailTableName");
            log.LogDebug($"Retrieving mail table: '{tableName}'");
            var mailTable = ServiceClient.GetTableClient(tableName);
            mailTable.CreateIfNotExists();

            log.LogDebug("Retrieving entity");
            var result = mailTable.GetEntityIfExists<MailTemplate>(MailTemplate.CreatePartitionKey(message.Lcid), message.TemplateType);
            if (result.HasValue)
            {
                return result.Value;
            }

            return new MailTemplate { Body = "body", Salution = "hoi", Subject = "onderwerp" };
            throw new FileNotFoundException($"Mail template not found in {tableName}.");
        }

        private bool IsRecipientBlackListed(string recipientAddress, ILogger log)
        {
            log.LogInformation($"Checking if recipient address {recipientAddress} is blacklisted");

            string tableName = GetEnvironmentVariable("BlackListTableName");
            log.LogDebug($"Retrieving blacklist table: '{tableName}'");
            var blackListTable = ServiceClient.GetTableClient(GetEnvironmentVariable("BlackListTableName"));
            blackListTable.CreateIfNotExists();

            log.LogDebug("Retrieving entity");
            var result = blackListTable.GetEntityIfExists<BlackListItem>("Email", recipientAddress.ToLowerInvariant());
            return result.HasValue;
        }

        private static string GetHtmlTemplate(Message message, ILogger log)
        {
            return GetHtmlTemplate(message.CheckBlacklist, message.Lcid, log);
        }

        private static string GetHtmlTemplate(bool checkBlacklist, int lcid, ILogger log)
        {
            string htmlTemplateFilename = checkBlacklist ? $"MailHTML-Public-{lcid}.html" : $"MailHTML-Users-{lcid}.html";
            log.LogInformation($"Retrieving email body html-file: {htmlTemplateFilename}");

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(typeof(Send), htmlTemplateFilename);
            if (stream == null)
            {
                throw new FileNotFoundException("Embedded html template file not found.");
            }

            string htmlTemplate = StreamToString(stream);
            return htmlTemplate;
        }

        private static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
