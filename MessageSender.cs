using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteServer
{
    internal class MessageSender
    {
        public static async Task SendMessageAsync(String message, Socket clientSocket)
        {
            try
            {
                ArraySegment<Byte> buffer = new ArraySegment<Byte>(Encoding.UTF8.GetBytes(message));
                await clientSocket.SendAsync(buffer, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sending message [{0}] has been failed. Address: {1}\nError: {2}", message, clientSocket.RemoteEndPoint, ex.Message);
                throw;
            }
        }
    }
}
