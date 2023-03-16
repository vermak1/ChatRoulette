using System;
using System.Net.Sockets;

namespace ChatRouletteServer
{
    internal class ClientInfo
    {
        public String Name { get; }
        public Socket ClientSocket { get; }

        public DateTime AddedTime { get; }
        public ClientInfo(String name, Socket socket)
        {
            Name = name;
            ClientSocket = socket;
            AddedTime = DateTime.Now;
        }

    }
}
