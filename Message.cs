using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageSender
{
    public class Message
    {
        /// <summary>
        /// Startparameter für App
        /// </summary>
        public string Launch { get; set; }

        /// <summary>
        /// Überschrift der Benachrichtigung
        /// </summary>
        public string Titel { get; set; }

        /// <summary>
        /// Inhalt der Benachrichtigung (optional)
        /// </summary>
        public string Inhalt { get; set; }

        /// <summary>
        /// Image der Benachrichtigung (optional)
        /// </summary>
        public string ImageSrc { get; set; }

        /// <summary>
        /// Inline-Image der Benachrichtigung (optional)
        /// </summary>
        public string InlineImageSrc { get; set; }
    }
}
