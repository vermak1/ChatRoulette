using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChatRouletteServer
{
    internal class ChatContextHolder
    {
        public Chat Chat { get; }

        public ChatContextHolder(Chat chat)
        {
            Chat = chat;
        }

        public event EventHandler<DisconnectEventArgs> Disconnected;

        private void OnDisconnected(DisconnectEventArgs eventArgs)
        {
            Disconnected?.Invoke(this, eventArgs);
        }

        public async Task RunChat()
        {
            await SendChatStartedMessage();
            ListenAndResend(Chat.Client1, Chat.Client2);
            ListenAndResend(Chat.Client2, Chat.Client1);
        }

        private async Task SendChatStartedMessage()
        {
            string Message(Client clientInfo) =>
                String.Format("You are chatting with {0}. Write message and press Enter to send it", clientInfo.Name);

            String s1 = Message(Chat.Client1);
            String s2 = Message(Chat.Client2);
            try
            {
                await MessageSender.SendMessageAsync(s1, Chat.Client2.ClientSocket);
                await MessageSender.SendMessageAsync(s2, Chat.Client1.ClientSocket);
            }
            catch (Exception)
            {
                OnDisconnected(new DisconnectEventArgs(Chat));
                throw;
            }
        }

        private void ListenAndResend(Client from, Client to)
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                while (true)
                {
                    try
                    {
                        String message = await MessageReceiver.ReceiveMessageAsync(from.ClientSocket);
                        await MessageSender.SendMessageAsync(message, to.ClientSocket);
                    }
                    catch (Exception)
                    {
                        OnDisconnected(new DisconnectEventArgs(Chat));
                        break;
                    }
                }
            });
        }
    }
}
