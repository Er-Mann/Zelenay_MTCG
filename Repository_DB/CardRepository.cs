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

                result.Add(card);
            }

            return result;
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
