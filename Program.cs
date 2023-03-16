using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatRouletteServer
{
    internal class Program
    {
        static async Task Main()
        {
            ServerSocket serverSocket;
            ClientChatManager manager = null;
            try
            {
                serverSocket = new ServerSocket();
                manager = new ClientChatManager(serverSocket);

                serverSocket.BindAndStartListen();
                manager.RunCheckAndMakeChatInCycle();
                manager.PingTimer(TimeSpan.FromSeconds(60));
                manager.RunCheckAndMakeChatInCycle();
                manager.ExceptionHandleCycle();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
            finally
            {
                manager?.Dispose();
            }
        }
    }
}
