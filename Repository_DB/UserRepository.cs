using System.Collections.Concurrent;
using System.Data;
using MonsterCardGame.Models.User;

namespace Zelenay_MTCG.Repository_DB
{
    public class UserRepository
    {
        private readonly string dbConnString;
        private readonly DBconn DBcs;

        // Konstruktor
        public UserRepository()
        {
            dbConnString = "Host=localhost;Database=mydb;Username=user;Password=password";
            DBcs = new DBconn(dbConnString);
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

        public User? GetUserById(int userId)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT userid, username, password, elo, gold, wins, losses, authtoken 
                FROM mydb.public.users 
                WHERE userid = @userid";

            AddParameterWithValue(command, "@userid", DbType.Int32, userId);

            using IDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Password = reader.GetString(2),
                    Elo = reader.GetInt32(3),
                    Gold = reader.GetInt32(4),
                    Wins = reader.GetInt32(5),
                    Losses = reader.GetInt32(6),
                    AuthToken = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
            }
            return null;
        }

        public User? GetUserByUsername(string username)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT userid, username, password, elo, gold, wins, losses, authtoken 
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
                    Password = reader.GetString(2),
                    Elo = reader.GetInt32(3),
                    Gold = reader.GetInt32(4),
                    Wins = reader.GetInt32(5),
                    Losses = reader.GetInt32(6),
                    AuthToken = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
            }
            return null;
        }

        public User? GetUserByAuthToken(string authToken)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using IDbCommand command = connection.CreateCommand();

            command.CommandText = """SELECT "userid", username, password, elo, gold, wins, losses, "authtoken" FROM mydb.public.users WHERE "authtoken" = @authtoken""";
            AddParameterWithValue(command, "@authtoken", DbType.String, authToken);

            using var reader = command.ExecuteReader(); if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Password = reader.GetString(2),
                    Elo = reader.GetInt32(3),
                    Gold = reader.GetInt32(4),
                    Wins = reader.GetInt32(5),
                    Losses = reader.GetInt32(6),
                    AuthToken = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
            }
            return null;
        }

        public void UpdateUser(User? user)
        {
            if (user == null || user.UserId == 0)
            {
                throw new InvalidDataException("User not found");
            }

            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using IDbCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText = @"UPDATE mydb.public.users SET elo = @elo, gold = @gold, wins = @wins, losses = @losses, ""authtoken"" = @authtoken WHERE ""userid"" = @userid";
                AddParameterWithValue(command, "@elo", DbType.Int32, user.Elo);
                AddParameterWithValue(command, "@gold", DbType.Int32, user.Gold);
                AddParameterWithValue(command, "@wins", DbType.Int32, user.Wins);
                AddParameterWithValue(command, "@losses", DbType.Int32, user.Losses);
                AddParameterWithValue(command, "@authtoken", DbType.String, user.AuthToken);
                AddParameterWithValue(command, "@userid", DbType.Int32, user.UserId);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdateUserWithConnection(User user, IDbConnection connection, IDbTransaction transaction)
        {
            if (user == null || user.UserId == 0)
            {
                throw new InvalidDataException("User not found");
            }

            using IDbCommand command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
            UPDATE mydb.public.users 
            SET elo = @elo, gold = @gold, wins = @wins, losses = @losses, ""authtoken"" = @authtoken 
            WHERE ""userid"" = @userid";

            AddParameterWithValue(command, "@elo", DbType.Int32, user.Elo);
            AddParameterWithValue(command, "@gold", DbType.Int32, user.Gold);
            AddParameterWithValue(command, "@wins", DbType.Int32, user.Wins);
            AddParameterWithValue(command, "@losses", DbType.Int32, user.Losses);
            AddParameterWithValue(command, "@authtoken", DbType.String, user.AuthToken);
            AddParameterWithValue(command, "@userid", DbType.Int32, user.UserId);

            command.ExecuteNonQuery();
        }


        //private user mapreadertouser(idatareader reader)
        //{
        //    return new user
        //    {
        //        userid = reader.getint32(0),
        //        username = reader.getstring(1),
        //        password = reader.getstring(2),
        //        elo = reader.getint32(3),
        //        gold = reader.getint32(4),
        //        wins = reader.getint32(5),
        //        losses = reader.getint32(6),
        //        authtoken = reader.isdbnull(7) ? string.empty : reader.getstring(7)
        //    };
        //}

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
