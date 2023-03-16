using System;
using System.Collections;
using System.Collections.Generic;

namespace ChatRouletteServer
{
    internal class Chat : IEnumerable<ClientInfo>
    {
        public ClientInfo Client1 { get; }
        public ClientInfo Client2 { get; }

        public Chat(ClientInfo client1, ClientInfo client2)
        {
            Client1 = client1;
            Client2 = client2;
        }

        public IEnumerator<ClientInfo> GetEnumerator() 
        {
            yield return Client1;
            yield return Client2;
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator(); 
    }
}
