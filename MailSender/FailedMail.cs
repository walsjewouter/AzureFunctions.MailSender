using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;

namespace MailSender
{
    public class FailedMail : TableEntity
    {
        public static readonly string PartitionKeyValue = "FailedMail";

        public FailedMail()
        {
            this.PartitionKey = PartitionKeyValue;
        }

        internal FailedMail(int language)
        {
            this.PartitionKey = PartitionKeyValue;
            this.RowKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.LanguageAsInt = language;
        }

        public string TemplateType { get; set; }
        public int LanguageAsInt { get; set; }
        public string Message { get; set; }
        public string Recipient { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionStackTrace { get; set; }
    }
}