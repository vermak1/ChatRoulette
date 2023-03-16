using System;
using System.Threading;

namespace ChatRouletteServer
{
    internal class DisconnectEventArgs : EventArgs
    {
        public Chat Chat { get; }


        public DisconnectEventArgs(Chat chat)
        {
            Chat = chat;
        }
    }
}
