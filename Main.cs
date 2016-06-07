using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Threading.Tasks;
using MessageSender.SendNotificationHandlers;

namespace MessageSender
{
    public class Main
    {
        private const string GCM_API_KEY = "AIzaSyDkGBerAss3gLM3sNHFxVfo4FNK8NHbGwM"; // "AIzaSyClmvvPFd5HnUsRlYbqkh7lo9gloUbeiYo";
        private const string UWP_SECRET = "bdvGokgtYLLioR5QvPftJUl69GI+ifXB";
        private const string UWP_SID = "ms-app://s-1-15-2-571355461-729806858-4070421896-1388646705-2215871882-4159891775-649992664";

        public const string DEFAULT_TOPIC = "global";

        private List<Client> Clients = new List<Client>();

        public Main()
        {
            //Clients aus Settings laden
            if (Properties.Settings.Default.Clients == null)
                SaveClients();  //Neu anlegen
            foreach (var ClientRaw in Properties.Settings.Default.Clients)
            {
                Clients.Add(JsonConvert.DeserializeObject<Client>(ClientRaw));
            }
            Console.WriteLine("{0} Clients registriert", Clients.Count);

            //Hauptschleife
            var readyToEnd = false;
            while(!readyToEnd)
            {
                Console.WriteLine("=== BITTE AUSWÄHLEN ===");
                Console.WriteLine(
                    "[0: Beenden, 1: Client hinzufügen, 2: Client entfernen, " +
                    "3: An alle senden, 4: Nur an Windows senden, 5: Nur an Apple senden, " +
                    "6: Nur an Android senden, 7: An Android-Topic senden]");
                char auswahl = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (auswahl)
                {
                    case '0':
                        readyToEnd = true;
                        break;
                    case '1':
                        AddClientPrompt();
                        break;
                    case '2':
                        RemoveClientPrompt();
                        break;
                    case '3':
                        Send(Clients);
                        break;
                    case '4':
                        SendMessageToWindows();
                        break;
                    case '5':
                        SendMessageToApple();
                        break;
                    case '6':
                        SendMessageToAndroid();
                        break;
                    case '7':
                        SendMessageToTopic();
                        break;
                    default:
                        Console.WriteLine("Ungültige Eingabe");
                        break;
                }
            }
            Console.WriteLine("Programm beendet; {0} Clients werden gespeichert; beliebige Taste zum Beenden drücken...", Clients.Count);
        }

        private void AddClientPrompt()
        {
            RETRY:
            Console.WriteLine("ClientType auswählen: [0: Android, 1: Apple, 2: Windows]");
            try
            {
                Client newClient = new Client();

                newClient.ClientType = (Client.ClientTypes)
                    Enum.Parse(typeof(Client.ClientTypes), Console.ReadKey().KeyChar.ToString());
                Console.WriteLine();

                Console.WriteLine("FriendlyName eingeben (Freitext):");
                newClient.FriendlyName = Console.ReadLine();

                Console.WriteLine("{0} eingeben:", newClient.GetTokenNameof());
                newClient.Token = Console.ReadLine();

                Clients.Add(newClient);
                SaveClients();
            }
            catch
            {
                Console.WriteLine("Ungültige Eingabe");
                goto RETRY;
            }
        }

        private void RemoveClientPrompt()
        {
            if (Clients.Count == 0)
            {
                Console.WriteLine("Keine Clients registriert");
                return;
            }

            Console.WriteLine("Nummer des Clients eingeben:");
            for(int i = 0; i < Clients.Count; i++)
            {
                Console.WriteLine("{0} {1}", i, Clients[i].ToString());
            }

            RETRY:
            try
            {
                int auswahl = int.Parse(Console.ReadLine());
                Clients.RemoveAt(auswahl);
                SaveClients();
            }
            catch
            {
                Console.WriteLine("Ungültige Eingabe");
                goto RETRY;
            }
        }

        private void SaveClients()
        {
            Properties.Settings.Default.Clients = new System.Collections.Specialized.StringCollection();
            foreach(var Client in Clients)
            {
                Properties.Settings.Default.Clients.Add(JsonConvert.SerializeObject(Client));
            }
            Properties.Settings.Default.Save();
        }

        private void Send(Client client)
        {
            Send(new Client[] { client });
        }

        private void Send(IList<Client> filteredClients)
        {
            Console.WriteLine("{0} Clients gefunden", filteredClients.Count);

            Message nachricht = MessagePrompt();

            Console.WriteLine("Sende Nachrichten an Clients...");

            Dictionary<Client.ClientTypes, ISendNotificationHandler> handlers = new Dictionary<Client.ClientTypes, ISendNotificationHandler>();
            for (int i = 0; i < filteredClients.Count; i++)
            {
                var client = filteredClients[i];
                Console.Write("Client {0}/{1} \"{2}\": ", i + 1, filteredClients.Count, client.FriendlyName);
                var handler = GetNotificationHandlerForClient(handlers, client);
                var result = handler.SendMessage(nachricht, client.Token);
                Console.Write(result.Success ? "OK" : ("ERROR: " + result.Error.Message));
                Console.Write(Environment.NewLine);
            }
        }

        private ISendNotificationHandler GetNotificationHandlerForClient(Dictionary<Client.ClientTypes, ISendNotificationHandler> handlers, Client client)
        {
            if (!handlers.ContainsKey(client.ClientType))
                handlers.Add(client.ClientType, CreateHandler(client.ClientType));
            return handlers[client.ClientType];
        }

        private ISendNotificationHandler CreateHandler(Client.ClientTypes clientType)
        {
            switch (clientType)
            {
                case Client.ClientTypes.Android:
                    return new GCMSendNotificationHandler(GCM_API_KEY);
                case Client.ClientTypes.Windows:
                    return new WNSSendNotificationHandler(UWP_SECRET, UWP_SID);
                case Client.ClientTypes.Apple:
                    return new APNSSendNotificationHandler();
                default:
                    throw new NotImplementedException();
            }
        }

        private Message MessagePrompt()
        {
            Message nachricht = new Message();

            //nachricht.ImageSrc = "ms-appx:///Images/Light/check.png";
            //"https://www.wien.gv.at/layout-a/wien.at/img/logo-wien.at.png"

            Console.WriteLine("Titel eingeben:");
            nachricht.Titel = Console.ReadLine();

            Console.WriteLine("Text eingeben (optional):");
            nachricht.Inhalt = Console.ReadLine();

            Console.WriteLine("ImageSrc eingeben (optional; verwende Web-URLs oder App-Pfad):");
            nachricht.ImageSrc = Console.ReadLine();

            nachricht.Launch = "testargument";

            return nachricht;
        }

        private void SendMessageToWindows()
        {
            var WindowsClients = (from Client in Clients
                                  where Client.ClientType == MessageSender.Client.ClientTypes.Windows
                                  select Client).ToArray();
            Send(WindowsClients);
        }

        private void SendMessageToApple()
        {
            var AppleClients = (from Client in Clients
                                  where Client.ClientType == MessageSender.Client.ClientTypes.Apple
                                  select Client).ToArray();
            Send(AppleClients);
        }

        private void SendMessageToAndroid()
        {
            var AndroidClients = (from Client in Clients
                                  where Client.ClientType == MessageSender.Client.ClientTypes.Android
                                  select Client).ToArray();
            Send(AndroidClients);
        }

        private void SendMessageToTopic()
        {
            Console.WriteLine("Topic eingeben (leer lassen für \"global\"):");
            var Topic = Console.ReadLine();
            if (string.IsNullOrEmpty(Topic))
                Topic = DEFAULT_TOPIC;

            Client topicClient = new Client() { Token = "/topics/" + Topic, ClientType = Client.ClientTypes.Android };

            var AndroidNotificationHandler = new GCMSendNotificationHandler(GCM_API_KEY);
            Send(topicClient);
        }
    }
}
