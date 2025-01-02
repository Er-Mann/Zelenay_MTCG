using System;
using System.IO;
using System.Net.Sockets;
using Zelenay_MTCG.Server.Endpoints.Userendpoint;

using Zelenay_MTCG.Server.Endpoints.Packageendpoint;
using Zelenay_MTCG.Server.HttpModel;
using Zelenay_MTCG.Server.HttpLogic;
using Zelenay_MTCG.Repository_DB;

namespace Zelenay_MTCG.Server.HttpHandler
{
    public class HttpProcessor
    {
        private readonly HttpRequest _requestHandler;
        private readonly HttpResponse _responseHandler;
        private readonly UserRepository _userRepository;
        private readonly PackageRepository _packageRepository;

        public HttpProcessor()
        {
            _requestHandler = new HttpRequest();
            _responseHandler = new HttpResponse();
            _userRepository = new UserRepository();
            _packageRepository = new PackageRepository();
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
            else if (request.Path == "/packages")
            {
                var userEndpoint = new PackageEndpoint(_packageRepository);
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
