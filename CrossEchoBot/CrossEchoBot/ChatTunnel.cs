using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossEchoBot
{
    class ChatTunnel
    {
        public ChatIdentity From { get; set; }

        public ChatIdentity To { get; set; }

        public ChatTunnel(ChatIdentity from, ChatIdentity to)
        {
            From = from;
            To = to;
        }
    }
}
