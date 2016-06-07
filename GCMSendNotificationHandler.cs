using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MessageSender.SendNotificationHandlers
{
    public class GCMSendNotificationHandler : ISendNotificationHandler
    {
        private string ApiKey;

        public GCMSendNotificationHandler(string ApiKey)
        {
            this.ApiKey = ApiKey;
        }

        public MessageSendingResult SendMessage(Message nachricht, string empfänger)
        {
            var jGcmData = new JObject();
            jGcmData.Add("to", empfänger);
            //jGcmData.Add("priority", "high");

            //var jNotification = new JObject();
            //jNotification.Add("message", nachricht.Inhalt); //test
            //jNotification.Add("body", nachricht.Inhalt);
            //jNotification.Add("title", nachricht.Titel);
            //jNotification.Add("icon", nachricht.ImageSrc);
            //jGcmData.Add("notification", jNotification);


            var jData = new JObject();
            jData.Add("message", nachricht.Inhalt); //test
            jData.Add("titel", nachricht.Titel);
            jData.Add("inhalt", nachricht.Inhalt);
            jData.Add("launch", nachricht.Launch);
            jData.Add("icon", nachricht.ImageSrc);
            jGcmData.Add("data", jData);

            var url = new Uri("https://gcm-http.googleapis.com/gcm/send");
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        "Authorization", "key=" + ApiKey);

                    var t = Task.Run(() => client.PostAsync(url,
                        new StringContent(jGcmData.ToString(), Encoding.Default, "application/json")));
                    t.Wait();
                    var response = t.Result;
                    
                    //Console.WriteLine(response);
                    
                    //TODO: Handle Web Status Codes
                    if (response.IsSuccessStatusCode)
                        return new MessageSendingResult(true);
                    else
                    {
                        return new MessageSendingResult(false, new Exception(response.Content.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                return new MessageSendingResult(false, ex);
            }
        }

    }


}
