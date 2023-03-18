using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatRouletteServer
{
    internal class Program
    {
        static void Main()
        {
            ServerSocket serverSocket = null;
            try
            {
                serverSocket = new ServerSocket();
                ClientChatManager manager = new ClientChatManager(serverSocket);
                manager.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
            finally
            {
                serverSocket?.Dispose();
            }
        }
    }
}
