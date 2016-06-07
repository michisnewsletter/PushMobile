using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageSender.SendNotificationHandlers
{
    public class MessageSendingResult
    {
        /// <summary>
        /// Ob Aufruf erfolgreich war
        /// </summary>
        public bool Success { get; }
        /// <summary>
        /// Fehler, der aufgetreten ist (kann null sein)
        /// </summary>
        public Exception Error { get; }

        public MessageSendingResult(bool Success, Exception Error = null)
        {
            this.Success = Success;
            this.Error = Error;
        }
    }

    public interface ISendNotificationHandler
    {
        MessageSendingResult SendMessage(Message nachricht, string empfänger);
    }
}
