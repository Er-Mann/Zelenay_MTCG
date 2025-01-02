using System;
using System.Net;
using System.Net.Sockets;
using Zelenay_MTCG.Server.HttpHandler;

namespace Zelenay_MTCG.Server
{
    public class TcpServer
    {
        private readonly TcpListener _httpServer;
        private readonly HttpProcessor _httpProcessor;

        public TcpServer(IPAddress ipAddress, int port)
        {
            _httpServer = new TcpListener(ipAddress, port);
            _httpProcessor = new HttpProcessor();
        }

        public void Start()
        {
            Console.WriteLine("Monster Card Game HTTP Server: http://localhost:10001/");

            _httpServer.Start();

            while (true)
            {
                var clientSocket = _httpServer.AcceptTcpClient();
                _httpProcessor.ProcessRequest(clientSocket);
            }
        }
    }
}
