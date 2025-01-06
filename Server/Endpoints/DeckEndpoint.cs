using System.Text;
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
            if (request.Path == "/deck" || request.Path == "/deck?format=plain")
            {
                if (request.Method == "GET")
                {
                    HandleGetDeck(request, response);
                }
                else if (request.Method == "PUT")
                {
                    HandleConfigureDeck(request, response);
                }
                else
                {
                    response.StatusCode = 405; // Method Not Allowed
                    response.Reason = "Method Not Allowed";
                }
            }
            else
            {
                response.StatusCode = 404;
                response.Reason = "Not Found";
            }
        }

        private void HandleGetDeck(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            string username = ExtractUsernameFromToken(authHeader);
            var user = _userRepository.GetUserByUsername(username);
            if (user == null)
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            var deck = _deckRepository.GetDeckByUserId(user.UserId);

            if (request.Path == "deck?format=plain")
            {
                var plainDeck = new StringBuilder();
                foreach (var card in deck)
                {
                    plainDeck.AppendLine($"{card.Id}: {card.Name} - Damage: {card.Damage} /n");
                }
                response.Body = plainDeck.ToString();
            }
            else
            {
                response.Body = JsonSerializer.Serialize(deck);
            }

            response.StatusCode = 200;
            response.Reason = "OK";
        }

        private void HandleConfigureDeck(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            string username = ExtractUsernameFromToken(authHeader);
            var user = _userRepository.GetUserByUsername(username);
            if (user == null)
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            List<string>? cardIds = JsonSerializer.Deserialize<List<string>>(request.Body);

            if (cardIds == null || cardIds.Count != 4)
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "You must provide exactly 4 card IDs.";
                return;
            }

            bool success = _deckRepository.ConfigureDeck(user.UserId, cardIds);

            if (!success)
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "Unable to configure deck.";
            }
            else
            {
                response.StatusCode = 200;
                response.Reason = "OK";
                response.Body = "Deck configured successfully.";
            }
        }

        private string ExtractUsernameFromToken(string authHeader)
        {
            if (authHeader.Contains("Bearer"))
            {
                string token = authHeader.Replace("Bearer", "").Trim();
                return token.Split("-")[0]; // e.g., "kienboec" from "kienboec-mtcgToken"
            }
            return string.Empty;
        }
    }
}
