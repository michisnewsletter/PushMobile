using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageSender
{
    [Serializable]
    public class Client
    {
        public class ClientInvalidException : Exception
        {
            public override string Message
            {
                get
                {
                    return "Dieser Client hat einen ungültigen Token/Channel und muss sich neu registrieren.";
                }
            }

            public ClientInvalidException(Exception InnerException) : base("", InnerException)
            {
            }
        }

        public enum ClientTypes
        {
            Android = 0,
            Apple = 1,
            Windows = 2
        }

        public ClientTypes ClientType { get; set; }

        public string GetTokenNameof()
        {
            switch(ClientType)
            {
                case ClientTypes.Android:
                case ClientTypes.Apple:
                    return "Token";
                case ClientTypes.Windows:
                    return "Channel-URL";
                default:
                    return "TOKEN";
            }
        }

        public string FriendlyName { get; set; }

        public string Token { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", FriendlyName, ClientType.ToString());
        }
    }
}
