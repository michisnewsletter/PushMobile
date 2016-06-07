using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Web;
using System.Net;
using Windows.Networking.PushNotifications;
using Windows.Foundation;
using System.Xml;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MessageSender.SendNotificationHandlers
{
    class WNSSendNotificationHandler : ISendNotificationHandler
    {   
        /// <summary>
        /// Eine Benachrichtigung zum Auslösen eines Popups auf dem Client.
        /// </summary>
        public const string NOTIFICATION_TYPE_TOAST = "wns/toast";
        /// <summary>
        /// Eine Benachrichtigung zum Aktualisieren des Kachelinhalts.
        /// </summary>
        public const string NOTIFICATION_TYPE_TILE = "wns/tile";
        /// <summary>
        /// Eine Benachrichtigung zum Erstellen einer Signalüberlagerung auf der Kachel.
        /// </summary>
        public const string NOTIFICATION_TYPE_BADGE = "wns/badge";
        public const string CONTENT_TYPE = "text/xml";
        public const string TEMPLATE_TOAST_IMAGE_AND_TEXT = "ToastImageAndText01";  //Text und Bild
        public const string TEMPLATE_TOAST_GENERIC = "ToastGeneric";    //Titel, Text und Bild (empfohlen)

        private string Secret;
        private string Sid;

        public WNSSendNotificationHandler(string Secret, string Sid)
        {
            this.Secret = Secret;
            this.Sid = Sid;
        }

        public MessageSendingResult SendMessage(Message nachricht, string empfänger)
        {
            string xml = GetXml(nachricht);

            return PostToWns(empfänger, xml, NOTIFICATION_TYPE_TOAST, CONTENT_TYPE);
        }


        /// <summary>
        /// Xml für Toast erstellen
        /// </summary>
        private string GetXml(Message nachricht)
        {
            StringWriter toastStringWriter = new StringWriter();
            XmlTextWriter toastWriter = new XmlTextWriter(toastStringWriter);
            toastWriter.Formatting = Formatting.Indented;

            toastWriter.WriteStartElement("toast");
            toastWriter.WriteAttributeString("launch", nachricht.Launch);

            toastWriter.WriteStartElement("visual");
            toastWriter.WriteStartElement("binding");
            toastWriter.WriteAttributeString("template", TEMPLATE_TOAST_GENERIC);
            toastWriter.WriteStartElement("text");
            toastWriter.WriteValue(nachricht.Titel);
            toastWriter.WriteEndElement();  //text
            toastWriter.WriteStartElement("text");
            toastWriter.WriteValue(nachricht.Inhalt);
            toastWriter.WriteEndElement();  //text
            if (!string.IsNullOrEmpty(nachricht.ImageSrc))
            {
                toastWriter.WriteStartElement("image");
                toastWriter.WriteAttributeString("placement", "AppLogoOverride");

                var imageuri = nachricht.ImageSrc;
                if (!nachricht.ImageSrc.StartsWith("http://") && !nachricht.ImageSrc.StartsWith("https://"))
                    imageuri = "ms-appx:///" + imageuri;
                toastWriter.WriteAttributeString("src", imageuri);
                toastWriter.WriteEndElement();  //image
            }
            if (!string.IsNullOrEmpty(nachricht.InlineImageSrc))
            {
                toastWriter.WriteStartElement("image");
                toastWriter.WriteAttributeString("placement", "inline");
                toastWriter.WriteAttributeString("src", nachricht.InlineImageSrc);
                toastWriter.WriteEndElement();  //image
            }
            toastWriter.WriteEndElement();  //binding
            toastWriter.WriteEndElement();  //visual

            //Actions TODO

            toastWriter.WriteEndElement();  //toast

            return toastStringWriter.ToString();
        }

        // Post to WNS
        private MessageSendingResult PostToWns(string channelUri, string xml, string notificationType, string contentType)
        {
            bool didRetry = false;
            RETRY:
            try
            {
                var accessToken = GetAccessToken();
                byte[] contentInBytes = Encoding.UTF8.GetBytes(xml);

                HttpWebRequest request = WebRequest.Create(channelUri) as HttpWebRequest;
                request.Method = "POST";
                request.Headers.Add("X-WNS-Type", notificationType);
                request.ContentType = contentType;
                request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken.AccessToken));

                using (Stream requestStream = request.GetRequestStream())
                    requestStream.Write(contentInBytes, 0, contentInBytes.Length);

                using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                {
                    //TODO: Handle Web Status Codes?
                    return new MessageSendingResult(true);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return new MessageSendingResult(false, ex);

                HttpStatusCode status = ((HttpWebResponse)ex.Response).StatusCode;

                if (status == HttpStatusCode.Unauthorized)
                {
                    // The access token you presented has expired. Get a new one and then try sending
                    // your notification again.

                    // Because your cached access token expires after 24 hours, you can expect to get 
                    // this response from WNS at least once a day.

                    GetAccessToken(true);

                    if (!didRetry)
                        goto RETRY;
                    else
                        return new MessageSendingResult(false, ex);
                }
                else if (status == HttpStatusCode.Gone || status == HttpStatusCode.NotFound)
                {
                    // The channel URI is no longer valid.

                    // Remove this channel from your database to prevent further attempts
                    // to send notifications to it.

                    // The next time that this user launches your app, request a new WNS channel.
                    // Your app should detect that its channel has changed, which should trigger
                    // the app to send the new channel URI to your app server.

                    return new MessageSendingResult(false, new Client.ClientInvalidException(ex));
                }
                else if (status == HttpStatusCode.NotAcceptable)
                {
                    // This channel is being throttled by WNS.

                    // Implement a retry strategy that exponentially reduces the amount of
                    // notifications being sent in order to prevent being throttled again.

                    // Also, consider the scenarios that are causing your notifications to be throttled. 
                    // You will provide a richer user experience by limiting the notifications you send 
                    // to those that add true value.

                    return new MessageSendingResult(false, ex);
                }
                else
                {
                    // WNS responded with a less common error. Log this error to assist in debugging.

                    // You can see a full list of WNS response codes here:
                    // http://msdn.microsoft.com/en-us/library/windows/apps/hh868245.aspx#wnsresponsecodes

                    Console.WriteLine(status.ToString());
                    Console.WriteLine(ex.Response.Headers["X-WNS-Debug-Trace"]);
                    Console.WriteLine(ex.Response.Headers["X-WNS-Error-Description"]);
                    Console.WriteLine(ex.Response.Headers["X-WNS-Msg-ID"]);
                    Console.WriteLine(ex.Response.Headers["X-WNS-Status"]);
                    return new MessageSendingResult(false, ex);
                }
            }

            catch (Exception ex)
            {
                return new MessageSendingResult(false, ex);
            }
        }
        
        #region Auth

        [DataContract]
        public class OAuthToken
        {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
        }

        private OAuthToken GetOAuthTokenFromJson(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(OAuthToken));
                var oAuthToken = (OAuthToken)ser.ReadObject(ms);
                return oAuthToken;
            }
        }

        protected OAuthToken GetAccessToken(bool forceRenew = false)
        {
            //Cache
            if (!forceRenew && !string.IsNullOrEmpty(Properties.Settings.Default.AccessToken))
                return GetOAuthTokenFromJson(Properties.Settings.Default.AccessToken);

            var urlEncodedSecret = HttpUtility.UrlEncode(Secret);
            var urlEncodedSid = HttpUtility.UrlEncode(Sid);

            var body =
              string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com",
              urlEncodedSid, urlEncodedSecret);

            string response;
            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                response = client.UploadString("https://login.live.com/accesstoken.srf", body);
            }

            //Cache
            Properties.Settings.Default.AccessToken = response;

            return GetOAuthTokenFromJson(response);
        }
        
        #endregion Auth
    }
}
