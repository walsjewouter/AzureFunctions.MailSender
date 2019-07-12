using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace MailSender
{
    public static class Send
    {
        private static CloudTableClient tableClient;

        [FunctionName("Send")]
        public static void Run([QueueTrigger("mailqueue", Connection = "AzureWebJobsStorage")]string queueItem, TraceWriter log)
        {
            log.Info("Queue trigger function processing queue item");

            log.Verbose("Restoring queue message object");
            var message = Message.FromJsonString(queueItem);
            var model = message.GetModel();
            var modelType = model.GetType();

            log.Verbose("Instantiating table client");
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnection"));
            tableClient = storageAccount.CreateCloudTableClient();

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
                var engine = GetRazorEngine(template, log);

                log.Verbose("Running razor engine");
                string subject = engine.Run("subject", modelType, model);
                string salution = engine.Run("salution", modelType, model);
                string body = engine.Run("body", modelType, model);

                // Get the correct base e-mail html
                var html = GetHtmlTemplate(message, log);
                // Replace tokens in HTML
                log.Verbose("Replacing tokens in html");
                html = html.Replace("[[%SALUTION%]]", salution);
                html = html.Replace("[[%BODY%]]", body);
                html = html.Replace("[[%BLACKLISTURL%]]", message.BlackListUrl);

                log.Verbose("Creating mail message");
                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(CloudConfigurationManager.GetSetting("SenderAddress"), CloudConfigurationManager.GetSetting("SenderName"));
                    mail.To.Add(new MailAddress(message.RecipientAddress));
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.IsBodyHtml = true;
                    mail.Subject = subject;
                    mail.Body = html;

                    log.Verbose("Instantiating SMTP client");
                    int mailPort = int.Parse(CloudConfigurationManager.GetSetting("MailPort"));
                    using (var smtpClient = new SmtpClient(CloudConfigurationManager.GetSetting("MailHost"), mailPort))
                    {
                        string mailServerUsername = CloudConfigurationManager.GetSetting("MailServerUsername");
                        if (!string.IsNullOrWhiteSpace(mailServerUsername))
                        {
                            smtpClient.Credentials = new NetworkCredential(mailServerUsername, CloudConfigurationManager.GetSetting("MailServerPassword"));
                        }

                        log.Info("Sending mail");
                        smtpClient.Send(mail);
                    }
                }
            }
            else
            {
                log.Warning($"Address is black listed, NOT sending mail");
            }

            log.Info("Queue trigger function processed queue item");
        }

        [FunctionName("SendFailed")]
        public static void RunPoison([QueueTrigger("mailqueue-poison", Connection = "AzureWebJobsStorage")]string queueItem, TraceWriter log)
        {
            log.Info("Queue trigger function processing queue-poison item");

            log.Verbose("Restoring failed queue message object");
            var failedMessage = Message.FromJsonString(queueItem);

            string subject = "MailSender failed";

            // Get the correct base e-mail html
            var html = GetHtmlTemplate(false, 1033, log);
            // Replace tokens in HTML
            log.Verbose("Replacing tokens in html");
            html = html.Replace("[[%SALUTION%]]", "Hi admin,");
            html = html.Replace("[[%BODY%]]", "<p>MailSender failed to send an email message.<br><strong>Please check the failed messages as soon as possible!</strong></p>");
            html = html.Replace("[[%BLACKLISTURL%]]", string.Empty);

            log.Verbose("Creating mail message");
            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress(CloudConfigurationManager.GetSetting("SenderAddress"), CloudConfigurationManager.GetSetting("SenderName"));
                mail.To.Add(new MailAddress(CloudConfigurationManager.GetSetting("FailedMailsRecipient")));
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.IsBodyHtml = true;
                mail.Subject = subject;
                mail.Body = html;

                log.Verbose("Instantiating SMTP client");
                int mailPort = int.Parse(CloudConfigurationManager.GetSetting("MailPort"));
                using (var smtpClient = new SmtpClient(CloudConfigurationManager.GetSetting("MailHost"), mailPort))
                {
                    string mailServerUsername = CloudConfigurationManager.GetSetting("MailServerUsername");
                    if (!string.IsNullOrWhiteSpace(mailServerUsername))
                    {
                        smtpClient.Credentials = new NetworkCredential(mailServerUsername, CloudConfigurationManager.GetSetting("MailServerPassword"));
                    }

                    log.Info("Sending mail");
                    smtpClient.Send(mail);
                }
            }

            var failedMail = new FailedMail(failedMessage.Lcid);
            failedMail.TemplateType = failedMessage.TemplateType;
            failedMail.Message = failedMessage != null ? failedMessage.ToJsonString() : null;
            failedMail.Recipient = failedMessage.RecipientAddress;
            failedMail.ExceptionMessage = null;
            failedMail.ExceptionStackTrace = null;

            var operation = TableOperation.Insert(failedMail);

            var mailTable = tableClient.GetTableReference(CloudConfigurationManager.GetSetting("MailTableName"));
            mailTable.CreateIfNotExists();
            mailTable.Execute(operation);

            log.Info("Queue trigger function processed queue-poison item");
        }

        private static bool IsRecipientBlackListed(string recipientAddress, TraceWriter log)
        {
            log.Info($"Checking if recipient address {recipientAddress} is blacklisted");

            var blackListTable = tableClient.GetTableReference(CloudConfigurationManager.GetSetting("BlackListTableName"));
            blackListTable.CreateIfNotExists();

            var retrieve = TableOperation.Retrieve<TableEntity>("Email", recipientAddress.ToLowerInvariant());
            var result = blackListTable.Execute(retrieve);
            return result.Result == null ? false : true;
        }

        private static IRazorEngineService GetRazorEngine(MailTemplate template, TraceWriter log)
        {
            log.Info("Instantiating Razor engine service");

            var config = new TemplateServiceConfiguration();

            // Set Custom assemblies reference resolver, to prevent "System.NotSupportedException" (The given path's format is not supported)
            config.ReferenceResolver = new CustomAssembliesReferenceResolver();
            config.DisableTempFileLocking = true;

            var engine = RazorEngineService.Create(config);

            engine.AddTemplate("subject", new LoadedTemplateSource(template.Subject));
            engine.Compile("subject");

            engine.AddTemplate("salution", new LoadedTemplateSource(template.Salution));
            engine.Compile("salution");

            engine.AddTemplate("body", new LoadedTemplateSource(template.Body));
            engine.Compile("body");

            return engine;
        }

        private static MailTemplate GetMailTemplate(Message message, TraceWriter log)
        {
            log.Info($"Retrieving mail text parts for template type {message.TemplateType} for language {message.Lcid}");

            string tableName = CloudConfigurationManager.GetSetting("MailTableName");
            var mailTable = tableClient.GetTableReference(tableName);
            mailTable.CreateIfNotExists();

            var retrieve = TableOperation.Retrieve<MailTemplate>(MailTemplate.CreatePartitionKey(message.Lcid), message.TemplateType);
            var result = mailTable.Execute(retrieve);

            if (result.Result != null)
            {
                return (MailTemplate)result.Result;
            }

            throw new FileNotFoundException($"Mail template not found in {tableName}.");
        }

        private static string GetHtmlTemplate(Message message, TraceWriter log)
        {
            return GetHtmlTemplate(message.CheckBlacklist, message.Lcid, log);
        }

        private static string GetHtmlTemplate(bool checkBlacklist, int lcid, TraceWriter log)
        {
            string htmlTemplate;
            string htmlTemplateFilename = checkBlacklist ? $"MailHTML-Public-{lcid}.html" : $"MailHTML-Users-{lcid}.html";
            log.Info($"Retrieving email body html-file: {htmlTemplateFilename}");

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(typeof(Send), htmlTemplateFilename))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Embedded html template file not found.");
                }

                htmlTemplate = StreamToString(stream);
            }

            return htmlTemplate;
        }

        private static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
