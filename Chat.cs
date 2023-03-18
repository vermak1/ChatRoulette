using System;
using System.Collections;
using System.Collections.Generic;

namespace ChatRouletteServer
{
    internal class Chat : IEnumerable<Client>
    {
        public Client Client1 { get; }
        public Client Client2 { get; }

        public Chat(Client client1, Client client2)
        {
            Client1 = client1;
            Client2 = client2;
        }

        public IEnumerator<Client> GetEnumerator() 
        {
            yield return Client1;
            yield return Client2;
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator(); 
    }
}
