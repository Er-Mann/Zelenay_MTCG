using System.Text.Json;
using Zelenay_MTCG.Models.Usermodel;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.HttpModel;

namespace Zelenay_MTCG.Server.Endpoints.DeckEndpoint
{
    public class DeckEndpoint : IEndpoint
    {
        private readonly DeckRepository _deckRepository;
        private readonly UserRepository _userRepository;

        public DeckEndpoint(DeckRepository deckRepo, UserRepository userRepo)
        {
            _deckRepository = deckRepo;
            _userRepository = userRepo;
        }

        public void HandleRequest(Request request, Response response)
        {
            // We only handle GET /deck here
            if (request.Method == "GET" && request.Path == "/deck")
            {
                // 1) Check token
                if (!request.Headers.TryGetValue("Authorization", out string authHeader))
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "Missing token.";
                    return;
                }

                // 2) Extract username from token
                string username = ExtractUsernameFromToken(authHeader);
                if (string.IsNullOrEmpty(username))
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "Invalid token.";
                    return;
                }

                // 3) Load user
                User? user = _userRepository.GetUserByUsername(username);
                if (user == null)
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "User not found.";
                    return;
                }

                // 4) Load deck from DB
                var deckCards = _deckRepository.GetDeckByUserId(user.UserId);

                // 5) Return deck as JSON
                response.StatusCode = 200;
                response.ReasonPhrase = "OK";
                response.Body = JsonSerializer.Serialize(deckCards);
            }
            else
            {
                // If not GET /deck, 404
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "Endpoint not found.";
            }
        }

        private string ExtractUsernameFromToken(string authHeader)
        {
            // e.g. "Bearer kienboec-mtcgToken"
            // a naive approach:
            if (authHeader.Contains("Bearer"))
            {
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
