using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlyMq.Adapter
{
    // State object for reading client data asynchronously  

    class Program
    {
        static List<Socket> _brokers = new List<Socket>();

        public static void StartServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.Count() > 1 ? ipHostInfo.AddressList[1] : ipHostInfo.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 6000);

            Socket adapter = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                adapter.Bind(ipEndPoint);
                adapter.Listen(100);

                Console.WriteLine($"Adapter[{adapter.LocalEndPoint}] is listening ...");

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += AcceptCallback;
                args.UserToken = adapter;

                if (!adapter.AcceptAsync(args))
                {
                    AcceptCallback(MethodBase.GetCurrentMethod().GetType(), args);
                };

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Enter Esc to exit other send messages to broker ...");
            SocketConsoler(adapter);
        }

        public static void SocketConsoler(Socket adapter)
        {
            if (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                _brokers.ForEach(broker =>
                     {
                         SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                         args.Completed += SendCallback;
                         args.AcceptSocket = broker;
                         byte[] data = Encoding.UTF8.GetBytes($"Adapter Date: {DateTime.Now}");
                         args.SetBuffer(data, 0, data.Length);
                         if (!broker.SendAsync(args))
                         {
                             SendCallback(MethodBase.GetCurrentMethod().GetType(), args);
                         }
                     });

                SocketConsoler(adapter);
            }
            else
            {
                adapter.Shutdown(SocketShutdown.Both);
                adapter.Close();
            }
        }

        public static void AcceptCallback(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                if (args.AcceptSocket != null && args.AcceptSocket.Connected)
                {
                    _brokers.Add(args.AcceptSocket);
                    Console.WriteLine($"Broker[{args.AcceptSocket.RemoteEndPoint}] connected ...");

                    SocketAsyncEventArgs rArgs = new SocketAsyncEventArgs();
                    rArgs.Completed += ReceiveCallback;
                    rArgs.AcceptSocket = args.AcceptSocket;
                    rArgs.UserToken = args.UserToken;
                    rArgs.SetBuffer(new byte[8912], 0, 8912);

                    if (!rArgs.AcceptSocket.ReceiveAsync(rArgs))
                    {
                        ReceiveCallback(MethodBase.GetCurrentMethod().GetType(), rArgs);
                    }
                }
                SocketAsyncEventArgs aArgs = new SocketAsyncEventArgs();
                aArgs.Completed += AcceptCallback;
                aArgs.UserToken = args.UserToken;
                (aArgs.UserToken as Socket).AcceptAsync(aArgs);
            }
            else
            {
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
                args.AcceptSocket.Close();
            }
        }

        public static void ReceiveCallback(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success && args.BytesTransferred > 0)
            {
                if (args.AcceptSocket.Available == 0)
                {
                    byte[] data = new byte[args.BytesTransferred];
                    Array.Copy(args.Buffer, args.Offset, data, 0, data.Length);
                    string info = Encoding.Default.GetString(data);
                    Console.WriteLine($"Receive from [{args.AcceptSocket.RemoteEndPoint}], Data: {info}");
                }

                if (!args.AcceptSocket.ReceiveAsync(args))
                {
                    ReceiveCallback(MethodBase.GetCurrentMethod().GetType(), args);
                }
            }
            else
            {
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
                args.AcceptSocket.Close();
            }
        }

        private static void SendCallback(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Console.WriteLine($"Send {args.BytesTransferred} byte to [{args.AcceptSocket.RemoteEndPoint}] success ...");
            }
            else
            {
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
                args.AcceptSocket.Close();
            }
        }

        static void Main(string[] args)
        {
            StartServer();
        }
    }
}
