using MessageSender.SendNotificationHandlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MessageSender
{
    public class APNSSendNotificationHandler : ISendNotificationHandler
    {
        const string certPath = "C:\\Projects\\MagWien.SagsWien\\MessageSender\\push.p12";
        public MessageSendingResult SendMessage(Message nachricht, string empfänger)
        {
            empfänger = empfänger.Replace(" ", "");

            int port = 2195;
            String hostname;
            //String certificatePath = System.Web.Hosting.HostingEnvironment.MapPath(certPath);
            X509Certificate2 clientCertificate = new X509Certificate2(System.IO.File.ReadAllBytes(certPath), "sagswien");
            X509Certificate2Collection certificatesCollection = new X509Certificate2Collection(clientCertificate);

            if (clientCertificate.ToString().Contains("Production"))
                hostname = "gateway.push.apple.com";
            else
                hostname = "gateway.sandbox.push.apple.com";
            
            using (TcpClient client = new TcpClient(hostname, port))
            {
                SslStream sslStream = new SslStream(client.GetStream(), false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

                try
                {
                    sslStream.AuthenticateAsClient(hostname, certificatesCollection, SslProtocols.Tls, false);
                    MemoryStream memoryStream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(memoryStream);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((byte)32);

                    writer.Write(HexStringToByteArray(empfänger));
                    String payload = "{\"aps\":{\"alert\":" + 
                        "{\"title\":\"" + nachricht.Titel + "\",\"body\":\"" + nachricht.Inhalt + "\"}" +
                        ",\"body\":\"" + nachricht.Inhalt + "\",\"launch\":\"" + nachricht.Launch + "\",\"badge\":1,\"sound\":\"default\"}}";
                    writer.Write((byte)0);
                    writer.Write((byte)payload.Length);
                    byte[] b1 = System.Text.Encoding.UTF8.GetBytes(payload);
                    writer.Write(b1);
                    writer.Flush();
                    byte[] array = memoryStream.ToArray();
                    sslStream.Write(array);
                    sslStream.Flush();

                    return new MessageSendingResult(true);
                }
                catch (AuthenticationException ex)
                {
                    return new MessageSendingResult(false, ex);
                }
                catch (Exception ex)
                {
                    return new MessageSendingResult(false, ex);
                }
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    
        private static bool ValidateServerCertificate(object a, X509Certificate b, X509Chain c, SslPolicyErrors d)
        {
            return true;
        }

    }
}
