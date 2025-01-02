using System;
using System.Net;
using MonsterCardGame.Server;
using Zelenay_MTCG.Repository_DB;

namespace MonsterCardGame
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
