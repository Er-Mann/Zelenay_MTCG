using System.Data;
using Npgsql;
using System.Collections.Generic;
using Zelenay_MTCG.Models.Cards;

namespace Zelenay_MTCG.Repository_DB
{
    public class CardRepository
    {
        private readonly DBconn _dbConn;

        public CardRepository()
        {
            _dbConn = new DBconn();
        }

        public List<Card> GetCardsByUserId(int userId)
        {
            var result = new List<Card>();

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT cardid, name, damage, element_type, card_type
        FROM cards
        WHERE userid = @userid
    ";

            AddParameter(cmd, "@userid", DbType.Int32, userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var card = new Card
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Damage = (float)reader.GetDecimal(2), // if damage is DECIMAL in DB
                    ElementType = (enumElementType)reader.GetInt32(3),
                    CardType = (enumCardType)reader.GetInt32(4)
                };

                // Ensure proper types are set if the database has missing or default values
                if (card.CardType == enumCardType.NotDefined || card.ElementType == default)
                {
                    AssignTypesFromName(card);
                }

                result.Add(card);
            }
            return result;
        }

        // Helper method to determine `CardType` and `ElementType` from the card's name
        private void AssignTypesFromName(Card card)
        {
            if (!string.IsNullOrEmpty(card.Name))
            {
                if (card.Name.Contains("Spell", StringComparison.OrdinalIgnoreCase))
                    card.CardType = enumCardType.Spell;
                else
                    card.CardType = enumCardType.Monster;

                if (card.Name.Contains("Fire", StringComparison.OrdinalIgnoreCase))
                    card.ElementType = enumElementType.Fire;
                else if (card.Name.Contains("Water", StringComparison.OrdinalIgnoreCase))
                    card.ElementType = enumElementType.Water;
                else
                    card.ElementType = enumElementType.Normal;
            }
            else
            {
                card.CardType = enumCardType.Monster;
                card.ElementType = enumElementType.Normal;
            }
        }


        private void AddParameter(IDbCommand command, string name, DbType type, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.DbType = type;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}
