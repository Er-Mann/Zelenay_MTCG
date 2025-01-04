using System.Text.Json;
using Zelenay_MTCG.Server.HttpModel;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.Endpoints;

using Zelenay_MTCG.Models.Usermodel;

namespace Zelenay_MTCG.Server.Endpoints.CardEndpoint
{
    public class CardEndpoint : IEndpoint
    {
        private readonly CardRepository _cardRepository;
        private readonly UserRepository _userRepository;

        public CardEndpoint(CardRepository cardRepository, UserRepository userRepository)
        {
            _cardRepository = cardRepository;
            _userRepository = userRepository;
        }

        public void HandleRequest(Request request, Response response)
        {
            if (request.Method == "GET" && request.Path == "/cards")
            {
                // 1) Check the auth token
                if (!request.Headers.TryGetValue("Authorization", out string authHeader))
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "Missing token.";
                    return;
                }

                string username = ExtractUsernameFromToken(authHeader);
                if (string.IsNullOrEmpty(username))
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "Invalid token.";
                    return;
                }

                // 2) Load user from DB
                User user = _userRepository.GetUserByUsername(username);
                if (user == null)
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "User not found.";
                    return;
                }

                // 3) Fetch cards from DB
                var cards = _cardRepository.GetCardsByUserId(user.UserId);

                // 4) Return them as JSON
                response.StatusCode = 200;
                response.ReasonPhrase = "OK";
                response.Body = JsonSerializer.Serialize(cards);
            }
            else
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "Endpoint not found.";
            }
        }

        private string ExtractUsernameFromToken(string authHeader)
        {
            // e.g. "Authorization: Bearer kienboec-mtcgToken"
            if (authHeader.Contains("Bearer"))
            {
                // naive approach:
                string tokenPart = authHeader.Replace("Bearer", "").Trim();
                // tokenPart = "kienboec-mtcgToken"
                int index = tokenPart.IndexOf("-mtcgToken", System.StringComparison.OrdinalIgnoreCase);
                if (index > 0)
                {
                    return tokenPart.Substring(0, index);
                }
            }
            return string.Empty;
        }
    }
}
