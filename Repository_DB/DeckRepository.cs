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

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT card_1_id, card_2_id, card_3_id, card_4_id
        FROM decks
        WHERE userid = @userid
        LIMIT 1
    ";
            AddParameter(cmd, "@userid", DbType.Int32, userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return deckCards; // No deck found, return empty list
            }

            var cardIds = new List<string?>()
    {
        reader.IsDBNull(0) ? null : reader.GetString(0),
        reader.IsDBNull(1) ? null : reader.GetString(1),
        reader.IsDBNull(2) ? null : reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3)
    };

            reader.Close(); // Ensure the reader is closed before executing new commands

            foreach (var cardId in cardIds.Where(id => id != null))
            {
                var card = LoadCardById(connection, cardId!);
                if (card != null)
                {
                    deckCards.Add(card);
                }
            }

            return deckCards;
        }


        public bool ConfigureDeck(int userId, List<string> cardIds)
        {
            if (cardIds.Count != 4)
            {
                return false; // Must have exactly 4 cards
            }

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO decks (userid, card_1_id, card_2_id, card_3_id, card_4_id)
                    VALUES (@userid, @card1, @card2, @card3, @card4)
                    ON CONFLICT (userid)
                    DO UPDATE SET
                        card_1_id = EXCLUDED.card_1_id,
                        card_2_id = EXCLUDED.card_2_id,
                        card_3_id = EXCLUDED.card_3_id,
                        card_4_id = EXCLUDED.card_4_id
                ";

                AddParameter(cmd, "@userid", DbType.Int32, userId);
                AddParameter(cmd, "@card1", DbType.String, cardIds[0]);
                AddParameter(cmd, "@card2", DbType.String, cardIds[1]);
                AddParameter(cmd, "@card3", DbType.String, cardIds[2]);
                AddParameter(cmd, "@card4", DbType.String, cardIds[3]);

                cmd.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private Card? LoadCardById(IDbConnection connection, string cardId)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT cardid, name, damage, element_type, card_type
                FROM cards
                WHERE cardid = @cardid
            ";
            AddParameter(cmd, "@cardid", DbType.String, cardId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new Card
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Damage = (float)reader.GetDecimal(2),
                ElementType = (enumElementType)reader.GetInt32(3),
                CardType = (enumCardType)reader.GetInt32(4)
            };
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
