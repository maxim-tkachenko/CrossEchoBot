using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossEchoBot
{
    class ChatIdentity
    {
        public string Id { get; set; }

        public string ServiceUrl { get; set; }

        public ChatIdentity(string id, string url)
        {
            Id = id;
            ServiceUrl = url;
        }
    }
}
