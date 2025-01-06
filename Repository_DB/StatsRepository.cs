using System.Data;
using Npgsql;
using Zelenay_MTCG.Models.Usermodel;

namespace Zelenay_MTCG.Repository_DB
{
    public class StatsRepository
    {
        private readonly DBconn _dbConn;

        public StatsRepository()
        {
            _dbConn = new DBconn();
        }

        public UserStats GetUserStats(string username)
        {
            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT username, elo, wins, losses
                FROM users
                WHERE username = @username
            ";
            AddParameter(cmd, "@username", DbType.String, username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserStats
                {
                    Username = reader.GetString(0),
                    Elo = reader.GetInt32(1),
                    Wins = reader.GetInt32(2),
                    Losses = reader.GetInt32(3)
                };
            }
            throw new Exception("User not found");
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

    public class UserStats
    {
        public string Username { get; set; }
        public int Elo { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }
}
