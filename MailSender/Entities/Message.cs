using Newtonsoft.Json;
using System;

namespace MailSender.Entities
{
    [Serializable]
    public class Message
    {
        public string TemplateType { get; set; }
        public int Lcid { get; set; }
        public bool CheckBlacklist { get; set; }
        public string BlackListUrl { get; set; }
        public string RecipientAddress { get; set; }
        public string JsonSerializedModel { get; set; }

        public static Message FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<Message>(jsonString);
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void SetModel(object model)
        {
            this.JsonSerializedModel = JsonConvert.SerializeObject(model);
        }

        public object GetModel()
        {
            return JsonConvert.DeserializeObject(this.JsonSerializedModel);
        }
    }
}
