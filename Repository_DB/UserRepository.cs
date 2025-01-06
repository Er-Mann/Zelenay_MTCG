using System.Collections.Concurrent;
using System.Data;
using Zelenay_MTCG.Models.Usermodel;

namespace Zelenay_MTCG.Repository_DB
{
    public class UserRepository
    {
        private readonly DBconn DBcs;

        // Konstruktor
        public UserRepository()
        {
            DBcs = new DBconn();
        }

        public void AddUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO mydb.public.users 
                (username, password) 
                VALUES (@username, @password) 
                RETURNING userid";

            AddParameterWithValue(command, "@username", DbType.String, user.Username);
            AddParameterWithValue(command, "@password", DbType.String, user.Password);

            user.UserId = Convert.ToInt32(command.ExecuteScalar());
        }



        public User? GetUserByUsername(string username)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT userid, username, password, name, bio, image, elo, wins, losses
        FROM users
        WHERE username = @username
    ";
            AddParameterWithValue(cmd, "@username", DbType.String, username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Password = reader.GetString(2),
                    Name = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Bio = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Image = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Elo = reader.GetInt32(6),
                    Wins = reader.GetInt32(7),
                    Losses = reader.GetInt32(8)
                };
            }

            return null;
        }


        public User? GetUserProfile(string username)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT userid, username, name, bio, image, elo, gold, wins, losses, authtoken
                FROM mydb.public.users
                WHERE username = @username";

            AddParameterWithValue(command, "@username", DbType.String, username);

            using IDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Bio = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Image = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Elo = reader.GetInt32(5),
                    Gold = reader.GetInt32(6),
                    Wins = reader.GetInt32(7),
                    Losses = reader.GetInt32(8),
                    AuthToken = reader.IsDBNull(9) ? "" : reader.GetString(9)
                };
            }
            return null;
        }

        public void UpdateUserProfile(int userId, string name, string bio, string image)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using IDbCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText = @"
            UPDATE mydb.public.users
            SET name = @name, bio = @bio, image = @image
            WHERE userid = @userid
        ";

                AddParameterWithValue(command, "@name", DbType.String, name);
                AddParameterWithValue(command, "@bio", DbType.String, bio);
                AddParameterWithValue(command, "@image", DbType.String, image);
                AddParameterWithValue(command, "@userid", DbType.Int32, userId);

                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdatePlayerStats(User user)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
        UPDATE mydb.public.users
        SET elo = @elo, wins = @wins, losses = @losses
        WHERE userid = @userid
    ";

            AddParameterWithValue(command, "@elo", DbType.Int32, user.Elo);
            AddParameterWithValue(command, "@wins", DbType.Int32, user.Wins);
            AddParameterWithValue(command, "@losses", DbType.Int32, user.Losses);
            AddParameterWithValue(command, "@userid", DbType.Int32, user.UserId);

            command.ExecuteNonQuery();
        }


        public static void AddParameterWithValue(IDbCommand command, string parameterName, DbType type, object value)
        {
            var parameter = command.CreateParameter();
            parameter.DbType = type;
            parameter.ParameterName = parameterName;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
