using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;


namespace ChatRouletteServer
{
    internal class ClientChatManager
    {
        private readonly List<Chat> _chats;
        private readonly List<Client> _clientPool;
        private readonly ServerSocket _serverSocket;

        public ClientChatManager(ServerSocket socket)
        {
            _chats = new List<Chat>(10);
            _clientPool = new List<Client>(20);
            _serverSocket = socket;
        }

        public void Start()
        {
            _serverSocket.BindAndStartListen();
            WaitingClientAndAddToPoolInCycle();
            RunCheckAndMakeChatInCycle();
            PingTimer(TimeSpan.FromSeconds(60));
            Thread.Sleep(Timeout.Infinite);
        }

        private void WaitingClientAndAddToPoolInCycle()
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                try
                {
                    while (true)
                    {
                        Console.WriteLine("Waiting for a new client...");
                        Socket clientSocket = await _serverSocket.AcceptAsync();

                        String name = await MessageReceiver.ReceiveMessageAsync(clientSocket);
                        Client client = new Client(name, clientSocket);
                        lock (_clientPool)
                        {
                            if (!_clientPool.Contains(client))
                                _clientPool.Add(client);
                        }
                        Console.WriteLine("User {0} has connected to server and added to pool.", name);
                        await SendWelcomeMessageAsync(client);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Waiting client cycle is failed: {0}", ex.Message);
                }
            });
        }

        private void RunCheckAndMakeChatInCycle()
        {
            ThreadPool.QueueUserWorkItem(async (obj) =>
            {
                try 
                { 
                    while (true)
                    {
                        (Client, Client) clientPair = (null, null);
                        lock (_clientPool)
                        {
                            if (_clientPool.Count >= 2)
                                clientPair = _clientPool.RandomPair();
                            else
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(10));
                                continue;
                            }
                        }

                        Boolean isClientsReady = false;
                        if (clientPair.Item1 != null && clientPair.Item2 != null)
                            isClientsReady = await CheckClientsBeforeChatStartAsync(clientPair);

                        Chat chat = null;
                        if(isClientsReady)
                        {
                            lock (_clientPool)
                            {
                                 chat = new Chat(clientPair.Item1, clientPair.Item2);
                                _clientPool.Remove(clientPair.Item1);
                                _clientPool.Remove(clientPair.Item2);
                                lock (_chats)
                                    _chats.Add(chat);
                            }
                            StartChat(chat);
                        }
                    }
                }
                catch(Exception ex) 
                {
                    Console.WriteLine("Making chat cycle if failed: {0}", ex.Message);
                }
            });
        }

        private void PingTimer(TimeSpan timeToPing)
        {
            ThreadPool.QueueUserWorkItem(async (obj) => 
            {
                try
                {
                    while(true)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(60));
                        foreach (var client in _clientPool.ToArray())
                        {
                            if (DateTime.Now - client.AddedTime > timeToPing)
                                await SendPingAsync(client);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ping sending failed: {0}", ex.Message);
                }
            });
        }

        private async Task SendPingAsync(Client client)
        {
            try
            {
                String message = "You are still is in the queue, please wait until someone connects";
                await MessageSender.SendMessageAsync(message, client.ClientSocket);
            }
            catch
            {
                throw;
            }
        }

        private async Task<Boolean> CheckClientsBeforeChatStartAsync((Client, Client) pair)
        {
            return await CheckClientInternalAsync(pair.Item1) && await CheckClientInternalAsync(pair.Item2);
        }

        private async Task<Boolean> CheckClientInternalAsync(Client client)
        {
            try
            {
                await MessageSender.SendMessageAsync("PingToStartChat", client.ClientSocket);
            }
            catch (SocketException)
            {
                Console.WriteLine("User {0} is unavailable and will be deleted from queue", client.Name);
                RemoveClientFromPool(client);
                return false;
            }
            return true;
        }

        private void RemoveClientFromPool(Client client)
        {
            lock (_clientPool)
            {
                client.Dispose();
                _clientPool.Remove(client);
            }
            Console.WriteLine("Client {0} was removed from pool", client.Name);
        }

        private void StartChat(Chat chat)
        {
            Console.WriteLine("New chat with users [{0}] and [{1}] has been created.", chat.Client1.Name, chat.Client2.Name);
            ThreadPool.QueueUserWorkItem(async (obj) =>
            {
                try
                {
                    ChatContextHolder chatContext = new ChatContextHolder(chat);
                    await chatContext.RunChat();
                    chatContext.Disconnected += HandleDisconnectEvent;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Chat with clients {0} and {1} has been destroyed\nError: {2}", chat.Client1.Name, chat.Client2.Name, ex.Message);
                }

            });
        }

        private async void HandleDisconnectEvent(object sender, DisconnectEventArgs eventArgs)
        {
            foreach (var client in eventArgs.Chat)
            {
                if (client.ClientSocket.Connected)
                    await ReturnClientToPool(client);
                else
                {
                    client.ClientSocket?.Dispose();
                    Console.WriteLine("User {0} has been disconnected and removed from chat roulette", client.Name);
                }
            }
            lock (_chats)
                _chats.Remove(eventArgs.Chat);
        }

        private async Task ReturnClientToPool(Client client)
        {
            Console.WriteLine("Client {0} will be returned to the pool", client.Name);
            lock (_clientPool)
                _clientPool.Add(client);

            String message = "Your interlocutor has been disconnected, you'll return back to queue";
            await MessageSender.SendMessageAsync(message, client.ClientSocket);
        }

        private async Task SendWelcomeMessageAsync(Client client)
        {
            try
            {
                String s = "Welcome to chat roulette, you are in queue for chatting...";
                await MessageSender.SendMessageAsync(s, client.ClientSocket);
            }
            catch
            {
                throw;
            }
        }
    }
}
