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
                response.Reason = "Not Found";
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
                response.Reason = "Unauthorized";
                response.Body = "User not found.";
                return;
            }

            var opponent = battleManager.DequeuePlayer();
            if (opponent == null)
            {
                battleManager.EnqueuePlayer(user);
                response.StatusCode = 202; // Accepted
                response.Reason = "Waiting for an opponent.";
                response.Body = "You have been added to the waiting list.";

                // Wait for the battle log to be available
                while (battleManager.GetBattleLogForPlayer(user.Username) == null)
                {
                    Thread.Sleep(100); // Avoid busy-waiting
                }

                // Retrieve the log after the battle is complete
                string? battleLog = battleManager.GetBattleLogForPlayer(user.Username);
                response.StatusCode = 200;
                response.Reason = "OK";
                response.Body = $"Battle completed! Log:\n{battleLog}";
            }
            else
            {
                string battleLog = StartBattle(user, opponent); // Define battle logic
                battleManager.AddBattleLog(user.Username, battleLog);
                battleManager.AddBattleLog(opponent.Username, battleLog);

                response.StatusCode = 200;
                response.Reason = "OK";
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

            string winner;
            if (deck1.Count > 0)
            {
                winner = player1.Username;
                UpdatePlayerStats(player1, player2, isDraw: false, isPlayer1Winner: true);
            }
            else if (deck2.Count > 0)
            {
                winner = player2.Username;
                UpdatePlayerStats(player1, player2, isDraw: false, isPlayer1Winner: false);
            }
            else
            {
                winner = "No one (draw)";
                UpdatePlayerStats(player1, player2, isDraw: true);
            }

            log.AppendLine($"{winner} wins the battle!");

            // Update database with new decks
            _deckRepository.ConfigureDeck(player1.UserId, deck1.Select(c => c.Id).ToList()); // Convert card objects to list of card IDs
            _deckRepository.ConfigureDeck(player2.UserId, deck2.Select(c => c.Id).ToList());

            return log.ToString();
        }

        private void UpdatePlayerStats(User player1, User player2, bool isDraw, bool isPlayer1Winner = false)
        {
            if (isDraw)
            {
                return; // No changes for a draw
            }

            if (isPlayer1Winner)
            {
                player1.Elo += 3;
                player1.Wins += 1;

                player2.Elo -= 5;
                player2.Losses += 1;
            }
            else
            {
                player2.Elo += 3;
                player2.Wins += 1;

                player1.Elo -= 5;
                player1.Losses += 1;
            }

            // Update players in the database
            _userRepository.UpdatePlayerStats(player1);
            _userRepository.UpdatePlayerStats(player2);
        }

        public float CalculateDamage(Card attacker, Card defender)
        {
            float baseDamage = attacker.Damage;
            float effectiveness = 1.0f;

            // Check if either card is a spell
            bool isSpellInvolved = attacker.CardType == enumCardType.Spell || defender.CardType == enumCardType.Spell;

            if (isSpellInvolved)
            {
                // Check effectiveness of Element 
                if (_effectivenessMultipliers.TryGetValue((attacker.ElementType, defender.ElementType), out float multiplier))
                {
                    effectiveness = multiplier;
                }
            }

            return baseDamage * effectiveness;
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

        private readonly Dictionary<(enumElementType, enumElementType), float> _effectivenessMultipliers = new()
        {
            {(enumElementType.Water, enumElementType.Fire), 1.5f},   // Water -> Fire
            {(enumElementType.Fire, enumElementType.Normal), 1.5f},  // Fire -> Normal
            {(enumElementType.Normal, enumElementType.Water), 1.5f}, // Normal -> Water
            {(enumElementType.Fire, enumElementType.Water), 0.5f},   // Fire -> Water
            {(enumElementType.Normal, enumElementType.Fire), 0.5f},  // Normal -> Fire
            {(enumElementType.Water, enumElementType.Normal), 0.5f}
        };
    }
}