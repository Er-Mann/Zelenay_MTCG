
using System.Net.Sockets;
using Zelenay_MTCG.Server.Endpoints.Userendpoint;
using Zelenay_MTCG.Server.Endpoints.TradeEndpoint;
using Zelenay_MTCG.Server.Endpoints.Packageendpoint;

using Zelenay_MTCG.Server.Endpoints.CardEndpoint;
using Zelenay_MTCG.Server.Endpoints.DeckEndpoint;
using Zelenay_MTCG.Server.HttpModel;
using Zelenay_MTCG.Server.HttpLogic;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.Endpoints.BattleEndpoint;
using Zelenay_MTCG.Server.Endpoints.StatsEndpoint;

using Zelenay_MTCG.Server.Endpoints.ScoreboardEndpoint;

namespace Zelenay_MTCG.Server.HttpHandler
{
    public class HttpProcessor
    {
        private readonly HttpRequest _requestHandler;
        private readonly HttpResponse _responseHandler;

        private readonly UserRepository _userRepository;
        private readonly PackageRepository _packageRepository;
        private readonly TradeRepository _TradeRepository;
        private readonly CardRepository _cardRepository;
        private readonly DeckRepository _deckRepository;
        private readonly StatsRepository _statsRepository; 
        private readonly ScoreboardRepository _scoreboardRepository;

        public HttpProcessor()
        {
            _requestHandler = new HttpRequest();
            _responseHandler = new HttpResponse();

            _userRepository = new UserRepository();
            _packageRepository = new PackageRepository();
            _TradeRepository = new TradeRepository(_userRepository);
            _cardRepository = new CardRepository();
            _deckRepository = new DeckRepository();
            _statsRepository = new StatsRepository();
            _scoreboardRepository = new ScoreboardRepository();
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
            else if (request.Path == "/transactions/packages")
            {
                var userEndpoint = new TradeEndpoint(_TradeRepository, _userRepository);
                userEndpoint.HandleRequest(request, response);
            }
            else if (request.Path == "/cards")
            {
                var cardEndpoint = new CardEndpoint(_cardRepository, _userRepository);
                cardEndpoint.HandleRequest(request, response);
            }
            else if (request.Path == "/deck" || request.Path == "/deck?format=plain")
            {
                var deckEndpoint = new DeckEndpoint(_deckRepository, _userRepository);
                deckEndpoint.HandleRequest(request, response);
            }
            else if (request.Path.StartsWith("/users/") && request.Method == "GET")
            {
                var userEndpoint = new UserEndpoint(_userRepository);
                userEndpoint.HandleGetUser(request, response);            //besser mit handlerequest machen
            }
            else if (request.Path.StartsWith("/users/") && request.Method == "PUT")
            {
                var userEndpoint = new UserEndpoint(_userRepository);
                userEndpoint.HandleUpdateUser(request, response);
            }
            else if (request.Path == "/battles" && request.Method == "POST")
            {
                var battleEndpoint = new BattleEndpoint(_userRepository, _deckRepository);
                battleEndpoint.HandleRequest(request, response);
            }
            else if (request.Path == "/stats" && request.Method == "GET")
            {
                var statsEndpoint = new StatsEndpoint(_statsRepository);
                statsEndpoint.HandleRequest(request, response);
            }
            else if (request.Path == "/scoreboard" && request.Method == "GET")
            {
                var scoreboardEndpoint = new ScoreboardEndpoint(_scoreboardRepository);
                scoreboardEndpoint.HandleRequest(request, response);
            }
            else
            {
                response.StatusCode = 404;
                response.Reason = "Not Found";
                response.Body = "The requested resource was not found.";
            }

            // Send the HTTP Response
            _responseHandler.SendResponse(writer, response);
        }
    }
}
