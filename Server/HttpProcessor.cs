using System;
using System.IO;
using System.Net.Sockets;
using MonsterCardGame.Server.Endpoints;
using MonsterCardGame.Server.HttpModel;
using MonsterCardGame.Server.HttpLogic;
using Zelenay_MTCG.Repository_DB;

namespace MonsterCardGame.Server.HttpHandler
{
    public class HttpProcessor
    {
        private readonly HttpRequest _requestHandler;
        private readonly HttpResponse _responseHandler;
        private readonly UserRepository _userRepository;

        public HttpProcessor()
        {
            _requestHandler = new HttpRequest();
            _responseHandler = new HttpResponse();
            _userRepository = new UserRepository();
        }

        public void ProcessRequest(TcpClient clientSocket)
        {
            using var networkStream = clientSocket.GetStream();
            using var reader = new StreamReader(networkStream);
            using var writer = new StreamWriter(networkStream) { AutoFlush = true };

            // Read the HTTP Request
            var request = _requestHandler.ReadRequest(reader);
            var response = new Response();
            if (request.Path == "/users" || request.Path == "/sessions")
            {
                var userEndpoint = new UserEndpoint(_userRepository);
                userEndpoint.HandleRequest(request, response);
            }
            else
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "The requested resource was not found.";
            }

            // Send the HTTP Response
            _responseHandler.SendResponse(writer, response);
        }
    }
}
