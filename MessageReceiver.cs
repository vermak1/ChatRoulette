using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteServer
{
    internal class MessageReceiver
    {
        private const Int32 BUFFER_SIZE = 512;
        public static async Task<String> ReceiveMessageAsync(Socket clientSocket)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new Byte[BUFFER_SIZE]);
            StringBuilder sb = new StringBuilder();
            try
            {
                do
                {
                    Int32 bytes = await clientSocket.ReceiveAsync(buffer, 0);
                    sb.Append(Encoding.UTF8.GetString(buffer.Array, 0, bytes));
                }
                while (clientSocket.Available != 0);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Receiving message failed, address: {0}\nError: {1}", clientSocket.RemoteEndPoint, ex.Message);
                throw;
            }
        }
    }
}
