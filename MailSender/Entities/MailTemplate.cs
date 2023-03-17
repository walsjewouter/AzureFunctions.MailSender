using Azure.Data.Tables;
using MailSender.Entities;

namespace MailSender
{
    public class MailTemplate : BaseTableEntity, ITableEntity
    {
        public string Subject { get; set; }
        public string Salution { get; set; }
        public string Body { get; set; }

        public static string CreatePartitionKey(int lcid)
        {
            return string.Concat("MailTemplate-", (int)lcid);
        }
    }
}
