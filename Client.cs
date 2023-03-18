using System;
using System.Net.Sockets;

namespace ChatRouletteServer
{
    internal class Client : IDisposable
    {
        public String Name { get; }
        public Socket ClientSocket { get; }

        public DateTime AddedTime { get; }
        public Client(String name, Socket socket)
        {
            Name = name;
            ClientSocket = socket;
            AddedTime = DateTime.Now;
        }

        public void Dispose() 
        {
            ClientSocket?.Dispose();
        }

    }
}
