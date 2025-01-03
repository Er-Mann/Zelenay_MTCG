using System;
using System.Net;
using Zelenay_MTCG.Server;
using Zelenay_MTCG.Repository_DB;
//Mein Projekt
namespace Zelenay_MTCG
{
    class Program
    {
        static void Main(string[] args)
        {
            DBconn DBcs = new DBconn();
            DBcs.CreateTables();

            var tcpServer = new TcpServer(IPAddress.Loopback, 10001);
            tcpServer.Start();
        }
    }
}
