using System.Data;
using Npgsql;
using Zelenay_MTCG.Models.Usermodel;


namespace Zelenay_MTCG.Repository_DB
{
    public class ScoreboardRepository
    {
        private readonly DBconn _dbConn;

        public ScoreboardRepository()
        {
            _dbConn = new DBconn();
        }

        public List<UserStats> GetScoreboard()
        {
            var scoreboard = new List<UserStats>();

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT username, elo, wins, losses
                FROM users
                ORDER BY elo DESC, wins DESC, losses ASC
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                scoreboard.Add(new UserStats
                {
                    Username = reader.GetString(0),
                    Elo = reader.GetInt32(1),
                    Wins = reader.GetInt32(2),
                    Losses = reader.GetInt32(3)
                });
            }

            return scoreboard;
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
