using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatRouletteServer
{
    internal class ChatContextHolder
    {
        private readonly CancellationTokenSource _cts;
        public Chat Chat { get; }


        public ChatContextHolder(Chat chat)
        {
            Chat = chat;
            _cts = new CancellationTokenSource();
        }

        public event EventHandler<DisconnectEventArgs> Disconnected;

        private void OnDisconnected(DisconnectEventArgs eventArgs)
        {
            Disconnected?.Invoke(this, eventArgs);
            _cts.Cancel();
        }

        public void ListenAndResend()
        {
            ListenAndResendInternal(Chat.Client1, Chat.Client2);
            ListenAndResendInternal(Chat.Client2, Chat.Client1);
        }

        public void SendChatStartedMessage()
        {
            string Message(ClientInfo clientInfo) =>
                String.Format("You are chatting with {0}. Write message and press Enter to send it", clientInfo.Name);

            String s1 = Message(Chat.Client1);
            String s2 = Message(Chat.Client2);
            try
            {
                SendMessageToClient(Chat.Client1, s2);
                SendMessageToClient(Chat.Client2, s1);
            }
            catch (SocketException)
            {
                OnDisconnected(new DisconnectEventArgs(Chat));
            }
        }

        private void ListenAndResendInternal(ClientInfo from, ClientInfo to)
        {
            ThreadPool.QueueUserWorkItem((tokenObj) =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        String message = GetMessageFromClient(from);
                        SendMessageToClient(to, message);
                    }
                    catch (SocketException)
                    {
                        OnDisconnected(new DisconnectEventArgs(Chat));
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            });
        }

        private String GetMessageFromClient(ClientInfo client)
        {
            Byte[] buffer = new Byte[ClientChatManager.BUFFER_SIZE];
            StringBuilder sb = new StringBuilder();
            try
            {
                do
                {
                    Int32 bytes = client.ClientSocket.Receive(buffer, ClientChatManager.BUFFER_SIZE, 0);
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, bytes));
                } 
                while (client.ClientSocket.Available != 0);
            }
            catch (SocketException)
            {
                throw;
            }
            return sb.ToString();
        }

        private void SendMessageToClient(ClientInfo client, String message)
        {
            try
            {
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                client.ClientSocket.Send(buffer, buffer.Length, SocketFlags.None);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client {0} can't be reached, interrupting chat", client.Name);
                _cts.Cancel();
            }
            catch(ObjectDisposedException)
            {
                Console.WriteLine("Client {0} was already disposed, interrupting chat", client.Name);
                _cts.Cancel();
            }
        }
    }
}
