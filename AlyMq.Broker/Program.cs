using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlyMq.Broker
{
    // State object for receiving data from remote device.  
    public class SocketState
    {
        // Client socket.  
        public Socket WorkSocket = default;
        // Size of receive buffer.  
        public const int bufferSize = 256;
        // Receive buffer.  
        public byte[] DataBuffer = new byte[bufferSize];
        // Received data string.  
        public StringBuilder DataString = new StringBuilder();
    }

    class Program
    {
        private static void StartClient()
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList.Count() > 1 ? ipHostInfo.AddressList[1] : ipHostInfo.AddressList[0];
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 6000);

                Socket client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                client.BeginConnect(ipEndPoint, ConnectCallback, client);

                Console.WriteLine("Enter Eec to exit other send messages to adapter ...");
                SocketConsoler(client);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SocketConsoler(Socket client)
        {
            if (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                byte[] byteData = Encoding.UTF8.GetBytes($"Mroker date time:{DateTime.Now}");
                client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);

                SocketConsoler(client);
            }
            else
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket workSocket = ar.AsyncState as Socket;
                workSocket.EndConnect(ar);
                Console.WriteLine("Socket connected to {0}", workSocket.RemoteEndPoint.ToString());


                SocketState state = new SocketState();
                state.WorkSocket = workSocket;
                workSocket.BeginReceive(state.DataBuffer, 0, SocketState.bufferSize, 0, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                SocketState state = ar.AsyncState as SocketState;
                Socket workSocket = state.WorkSocket;

                int bytesRead = workSocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    string dataString = Encoding.UTF8.GetString(state.DataBuffer, 0, bytesRead);
                    Console.WriteLine("Received : {0}", dataString);
                    workSocket.BeginReceive(state.DataBuffer, 0, SocketState.bufferSize, 0, ReceiveCallback, state);
                }
                else
                {
                    Console.WriteLine($"Broker[{workSocket.RemoteEndPoint}] is closed ...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket workSocket = ar.AsyncState as Socket;
                int bytesSent = workSocket.EndSend(ar);
                Console.WriteLine($"Sent {bytesSent} bytes to Adapter[{workSocket.RemoteEndPoint}] ....");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            StartClient();
        }
    }
}
