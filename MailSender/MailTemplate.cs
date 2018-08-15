using Microsoft.WindowsAzure.Storage.Table;

namespace MailSender
{
    public class MailTemplate : TableEntity
    {
        public MailTemplate()
        {
        }

        public string Subject { get; set; }
        public string Salution { get; set; }
        public string Body { get; set; }

        public static string CreatePartitionKey(int lcid)
        {
            return string.Concat("MailTemplate-", (int)lcid);
        }
    }
}
