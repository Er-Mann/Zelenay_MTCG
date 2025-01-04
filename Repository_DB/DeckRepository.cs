using System.Data;
using Npgsql;
using Zelenay_MTCG.Models.Cards;

namespace Zelenay_MTCG.Repository_DB
{
    public class DeckRepository
    {
        private readonly DBconn _dbConn;

        public DeckRepository()
        {
            _dbConn = new DBconn();
        }

        public List<Card> GetDeckByUserId(int userId)
        {
            var deckCards = new List<Card>();

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            // 1) Load the deck row for this user
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT deckid, card_1_id, card_2_id, card_3_id, card_4_id
                FROM decks
                WHERE userid = @userid
                LIMIT 1
            ";
            AddParameter(cmd, "@userid", DbType.Int32, userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                // no row => no deck => return empty list
                return deckCards;
            }

            // If we found a row, gather the card IDs
            string? card1Id = reader.IsDBNull(1) ? null : reader.GetString(1);
            string? card2Id = reader.IsDBNull(2) ? null : reader.GetString(2);
            string? card3Id = reader.IsDBNull(3) ? null : reader.GetString(3);
            string? card4Id = reader.IsDBNull(4) ? null : reader.GetString(4);

            reader.Close(); // done with deck query

            // 2) For each cardId that is not null, load the card from 'cards' table
            var allCardIds = new[] { card1Id, card2Id, card3Id, card4Id }
                .Where(id => id != null)
                .ToList();

            foreach (var cardId in allCardIds)
            {
                Card? card = LoadCardById(connection, cardId!);
                if (card != null)
                    deckCards.Add(card);
            }

            return deckCards;
        }

        private Card? LoadCardById(IDbConnection connection, string cardId)
        {
            // minimal inline approach
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT cardid, name, damage, element_type, card_type
                FROM cards
                WHERE cardid = @cardid
            ";
            AddParameter(cmd, "@cardid", DbType.String, cardId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var card = new Card
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Damage = (float)reader.GetDecimal(2),
                ElementType = (enumElementType)reader.GetInt32(3),
                CardType = (enumCardType)reader.GetInt32(4)
            };
            return card;
        }

        private void AddParameter(IDbCommand command, string name, DbType type, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.DbType = type;
            param.Value = value ?? DBNull.Value;
            command.Parameters.Add(param);
        }
    }
}
