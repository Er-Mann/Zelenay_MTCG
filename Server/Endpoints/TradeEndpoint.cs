using System.Text.Json;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.HttpModel;
using Zelenay_MTCG.Models.Usermodel;
using Zelenay_MTCG.Models.Cards;
using System.Diagnostics;

using Zelenay_MTCG.Models.TradeModel;

namespace Zelenay_MTCG.Server.Endpoints.TradeEndpoint
{
    public class TradeEndpoint : IEndpoint
    {
        private readonly TradeRepository _tradeRepo;
        private readonly UserRepository _userRepo;

        public TradeEndpoint(TradeRepository tradeRepo, UserRepository userRepo)
        {
            _tradeRepo = tradeRepo;
            _userRepo = userRepo;
        }

        public void HandleRequest(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                response.Body = "Missing authentication token.";
                return;
            }

            string username = ExtractUsernameFromToken(authHeader);
            if (string.IsNullOrEmpty(username))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            if (request.Method == "GET")
            {
                HandleGetTrades(response);
            }
            else if (request.Method == "POST")
            {
                if (request.Path == "/tradings")
                {
                    HandleCreateTrade(request, response, username);
                }
                else if (request.Path == "/transactions/packages")
                {
                    HandleAcquirePackage(request, response, username);
                }
                else
                {
                    HandleExecuteTrade(request, response, username);
                }
            }
            else if (request.Method == "DELETE")
            {
                HandleDeleteTrade(request, response, username);
            }
            else
            {
                response.StatusCode = 405;
                response.Reason = "Method Not Allowed";
            }
        }

        private void HandleGetTrades(Response response)
        {
            var trades = _tradeRepo.GetAllTrades();
            response.StatusCode = 200;
            response.Reason = "OK";
            response.Body = JsonSerializer.Serialize(trades);
        }

        private void HandleCreateTrade(Request request, Response response, string username)
        {
            var tradeData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.Body);

            if (tradeData == null || !tradeData.ContainsKey("Id") || !tradeData.ContainsKey("CardToTrade") ||
                !tradeData.ContainsKey("Type") || !tradeData.ContainsKey("MinimumDamage"))
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "Invalid trade data.";
                return;
            }

            // Convert trade type to enum
            int tradeType = tradeData["Type"].GetString().Equals("monster", StringComparison.OrdinalIgnoreCase) ? 1 :
                            tradeData["Type"].GetString().Equals("spell", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

            if (tradeType == 0)
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "Invalid card type.";
                return;
            }

            // Extract MinimumDamage safely as an integer
            int minimumDamage = tradeData["MinimumDamage"].GetInt32();

            var trade = new Trade(
                tradeId: tradeData["Id"].GetString(),
                seller: username,
                cardToTrade: tradeData["CardToTrade"].GetString(),
                cardType: tradeType,
                minimumDamage: minimumDamage
            );

            if (!_tradeRepo.CreateTrade(username, trade))
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "Trade creation failed.";
                return;
            }

            response.StatusCode = 201;
            response.Reason = "Created";
            response.Body = "Trade created successfully.";
        }



        private void HandleExecuteTrade(Request request, Response response, string username)
        {
            string tradeId = request.Path.Split('/').Last();
            string offeredCardId = JsonSerializer.Deserialize<string>(request.Body);

            if (_tradeRepo.ExecuteTrade(username, tradeId, offeredCardId))
            {
                response.StatusCode = 201;
                response.Reason = "Created";
                response.Body = "Trade executed successfully.";
            }
            else
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "Trade execution failed.";
            }
        }

        private void HandleDeleteTrade(Request request, Response response, string username)
        {
            string tradeId = request.Path.Split('/').Last();

            if (_tradeRepo.DeleteTrade(username, tradeId))
            {
                response.StatusCode = 200;
                response.Reason = "OK";
                response.Body = "Trade deleted successfully.";
            }
            else
            {
                response.StatusCode = 403;
                response.Reason = "Forbidden";
                response.Body = "Unauthorized to delete this trade or trade not found.";
            }
        }

        private void HandleAcquirePackage(Request request, Response response, string username)
        {
            User user = _userRepo.GetUserByUsername(username);
            if (user == null)
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                response.Body = "User not found.";
                return;
            }

            bool success = _tradeRepo.AcquirePackage(user);
            if (!success)
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "Not enough money or no packages available.";
            }
            else
            {
                response.StatusCode = 201;
                response.Reason = "Created";
                response.Body = "Package acquired.";
            }
        }

        private string ExtractUsernameFromToken(string authHeader)
        {
            if (authHeader.Contains("Bearer"))
            {
                string token = authHeader.Replace("Bearer", "").Trim();
                return token.Split("-")[0];
            }
            return string.Empty;
        }
    }
}
