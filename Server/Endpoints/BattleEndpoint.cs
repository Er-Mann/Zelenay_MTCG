using System.Text;
using Zelenay_MTCG.Models.Cards;
using Zelenay_MTCG.Models.Usermodel;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.Battle;
using Zelenay_MTCG.Server.HttpModel;

namespace Zelenay_MTCG.Server.Endpoints.BattleEndpoint
{
    public class BattleEndpoint : IEndpoint
    {
        private readonly UserRepository _userRepository;
        private readonly DeckRepository _deckRepository;

        public BattleEndpoint(UserRepository userRepository, DeckRepository deckRepository)
        {
            _userRepository = userRepository;
            _deckRepository = deckRepository;
        }

        public void HandleRequest(Request request, Response response)
        {
            if (request.Path == "/battles" && request.Method == "POST")
            {
                HandleBattle(request, response);
            }
            else
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
            }
        }

        private void HandleBattle(Request request, Response response)
        {
            var battleManager = BattleManager.Instance;
            string username = ExtractUsernameFromToken(request.Headers["Authorization"]);
            var user = _userRepository.GetUserByUsername(username);

            if (user == null)
            {
                response.StatusCode = 401;
                response.ReasonPhrase = "Unauthorized";
                response.Body = "User not found.";
                return;
            }

            var opponent = battleManager.DequeuePlayer();
            if (opponent == null)
            {
                battleManager.EnqueuePlayer(user);
                response.StatusCode = 202; // Accepted
                response.ReasonPhrase = "Waiting for an opponent.";
                response.Body = "You have been added to the waiting list.";

                // Wait for the battle log to be available
                while (battleManager.GetBattleLogForPlayer(user.Username) == null)
                {
                    Thread.Sleep(100); // Avoid busy-waiting
                }

                // Retrieve the log after the battle is complete
                string? battleLog = battleManager.GetBattleLogForPlayer(user.Username);
                response.StatusCode = 200;
                response.ReasonPhrase = "OK";
                response.Body = $"Battle completed! Log:\n{battleLog}";
            }
            else
            {
                string battleLog = StartBattle(user, opponent); // Define battle logic
                battleManager.AddBattleLog(user.Username, battleLog);
                battleManager.AddBattleLog(opponent.Username, battleLog);

                response.StatusCode = 200;
                response.ReasonPhrase = "OK";
                response.Body = $"Battle completed! Log:\n{battleLog}";
            }
        }

        private string StartBattle(User player1, User player2)
        {
            var deck1 = _deckRepository.GetDeckByUserId(player1.UserId);
            var deck2 = _deckRepository.GetDeckByUserId(player2.UserId);

            if (deck1.Count == 0 || deck2.Count == 0)
                return "One or both players have no cards in their deck.";

            var log = new StringBuilder();
            log.AppendLine($"Battle between {player1.Username} and {player2.Username}");

            int rounds = 0;
            while (deck1.Count > 0 && deck2.Count > 0 && rounds < 100)
            {
                rounds++;
                var card1 = deck1[new Random().Next(deck1.Count)];
                var card2 = deck2[new Random().Next(deck2.Count)];

                float damage1 = CalculateDamage(card1, card2);
                float damage2 = CalculateDamage(card2, card1);

                if (damage1 > damage2)
                {
                    log.AppendLine($"Round {rounds}: {card1.Name} defeats {card2.Name}");
                    deck2.Remove(card2);
                    deck1.Add(card2); // Transfer card 
                }
                else if (damage1 < damage2)
                {
                    log.AppendLine($"Round {rounds}: {card2.Name} defeats {card1.Name}");
                    deck1.Remove(card1);
                    deck2.Add(card1); // Transfer card
                }
                else
                {
                    log.AppendLine($"Round {rounds}: {card1.Name} vs {card2.Name} ends in a draw");
                }
            }

            string winner = deck1.Count > 0 ? player1.Username : player2.Username;
            log.AppendLine($"{winner} wins the battle!");

            // Update database with new decks
            _deckRepository.ConfigureDeck(player1.UserId, deck1.Select(c => c.Id).ToList());
            _deckRepository.ConfigureDeck(player2.UserId, deck2.Select(c => c.Id).ToList());

            return log.ToString();
        }

        private float CalculateDamage(Card attacker, Card defender)
        {
            //if (attacker.CardType == enumCardType.Spell && defender.CardType == enumCardType.Spell) //blödsinn????
            //{
            //    if (attacker.ElementType == enumElementType.Fire && defender.ElementType == enumElementType.Water)
            //        return attacker.Damage / 2;
            //    if (attacker.ElementType == enumElementType.Water && defender.ElementType == enumElementType.Fire)
            //        return attacker.Damage * 2;
            //}

            return attacker.Damage; // Base damage for all other cases
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