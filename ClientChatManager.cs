using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.ExceptionServices;

namespace ChatRouletteServer
{
    internal class ClientChatManager : IDisposable
    {
        private readonly List<Chat> _chats;
        private readonly List<ClientInfo> _clientPool;
        private readonly ServerSocket _serverSocket;
        public readonly static Int32 BUFFER_SIZE = 512;
        private ExceptionDispatchInfo _dispatchInfo;

        public ClientChatManager(ServerSocket socket)
        {
            _chats = new List<Chat>(10);
            _clientPool = new List<ClientInfo>(20);
            _serverSocket = socket;
        }

        public void WaitingClientAndAddToPoolInCycleAsync()
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                try
                {
                    while (true)
                    {
                        Console.WriteLine("Waiting for a new client...");
                        Socket clientSocket = await _serverSocket.AcceptAsync();

                        String name = await GetClientNameAsync(clientSocket);
                        ClientInfo client = new ClientInfo(name, clientSocket);
                        lock (_clientPool)
                            _clientPool.Add(client);
                        Console.WriteLine("User {0} has connected to server and added to pool.", name);
                        await SendWelcomeMessageAsync(client);
                    }
                }
                catch (Exception ex) 
                {
                    Console.WriteLine("Waiting client cycle is failed: {0}", ex.Message);
                    _dispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
            });
        }

        public void RunCheckAndMakeChatInCycle()
        {
            ThreadPool.QueueUserWorkItem(async (obj) =>
            {
                try 
                { 
                    while (true)
                    {
                        if (_clientPool.Count >= 2)
                        {
                            (ClientInfo, ClientInfo) clientPair = _clientPool.ToArray().RandomPair();
                            if (await CheckClientsBeforeChatStartAsync(clientPair))
                            {
                                Chat chat = new Chat(clientPair.Item1, clientPair.Item2);
                                lock (_chats)
                                    _chats.Add(chat);
                                lock (_clientPool)
                                {
                                    _clientPool.Remove(clientPair.Item1);
                                    _clientPool.Remove(clientPair.Item2);
                                }
                                StartChat(chat);
                            }
                        }
                        else
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
                catch(Exception ex) 
                {
                    Console.WriteLine("Making chat cycle if failed: {0}", ex.Message);
                    _dispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
            });
        }

        public void PingTimer(TimeSpan timeToPing)
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
                    _dispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
            });
        }

        public void ExceptionHandleCycle()
        {
            while (true)
            {
                if (_dispatchInfo != null)
                    _dispatchInfo.Throw();
            }
        }

        private async Task SendPingAsync(ClientInfo client)
        {
            try
            {
                String message = "You are still is in the queue, please wait until someone connects";
                await SendMessageAsync(client, message);
            }
            catch
            {
                throw;
            }
        }

        private async Task<Boolean> CheckClientsBeforeChatStartAsync((ClientInfo, ClientInfo) pair)
        {
            return await CheckClientInternalAsync(pair.Item1) && await CheckClientInternalAsync(pair.Item2);
        }

        private async Task<Boolean> CheckClientInternalAsync(ClientInfo client)
        {
            try
            {
                await SendMessageAsync(client, "PingToStartChat");
            }
            catch (SocketException)
            {
                Console.WriteLine("User {0} is unavailable and will be deleted from queue", client.Name);
                RemoveClientFromPool(client);
                return false;
            }
            return true;
        }

        private void RemoveClientFromPool(ClientInfo client)
        {
            lock (_clientPool)
                _clientPool.Remove(client);
            Console.WriteLine("Client {0} was removed from pool", client.Name);
        }

        private async Task SendMessageAsync(ClientInfo client, String message)
        {
            try
            {
                ArraySegment<Byte> buffer = new ArraySegment<Byte>(Encoding.UTF8.GetBytes(message));
                await client.ClientSocket.SendAsync(buffer, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sending message [{0}] to client {1} is failed\nError: {2}", message, client.Name, ex.Message);
                RemoveClientFromPool(client);
            }
        }

        private void StartChat(Chat chat)
        {
            Console.WriteLine("New chat with users [{0}] and [{1}] has been created.", chat.Client1.Name, chat.Client2.Name);
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                ChatContextHolder chatContext = new ChatContextHolder(chat);
                chatContext.SendChatStartedMessage();
                chatContext.ListenAndResend();
                chatContext.Disconnected += HandleDisconnectEvent;
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

        private async Task ReturnClientToPool(ClientInfo client)
        {
            Console.WriteLine("Client {0} will be returned to the pool", client.Name);
            lock (_clientPool)
                _clientPool.Add(client);
            String message = "Your interlocutor has been disconnected, you'll return back to queue";
            await SendMessageAsync(client, message);
        }

        private async Task SendWelcomeMessageAsync(ClientInfo client)
        {
            try
            {
                String s = "Welcome to chat roulette, you are in queue for chatting...";
                await SendMessageAsync(client, s);
            }
            catch
            {
                throw;
            }
        }

        private async Task<String> GetClientNameAsync(Socket clientSocket)
        {
            Byte[] buffer = new Byte[BUFFER_SIZE];
            ArraySegment<Byte> bufferArr = new ArraySegment<Byte>(buffer);
            Int32 bytes = await clientSocket.ReceiveAsync(bufferArr, 0);
            return Encoding.UTF8.GetString(bufferArr.Array, 0, bytes);
        }

        public void Dispose()
        {
            _serverSocket?.Dispose();
        }
    }
}
