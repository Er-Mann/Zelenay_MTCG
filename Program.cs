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
            var dbConnString = "Host=localhost;Database=mydb;Username=user;Password=password";
            DBconn DBcs = new DBconn(dbConnString);
            DBcs.CreateTables();

            var tcpServer = new TcpServer(IPAddress.Loopback, 10001);
            tcpServer.Start();
        }
    }
}
