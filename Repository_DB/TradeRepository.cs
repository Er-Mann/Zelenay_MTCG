using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Zelenay_MTCG.Models.Usermodel;
using Zelenay_MTCG.Models.TradeModel;
using Zelenay_MTCG.Models.Cards;

namespace Zelenay_MTCG.Repository_DB
{
    public class TradeRepository
    {
        private readonly DBconn _dbConn;
        private readonly UserRepository _userRepo;

        public TradeRepository(UserRepository userRepo)
        {
            _dbConn = new DBconn();
            _userRepo = userRepo;
        }

        public bool AcquirePackage(User user)
        {
            const int PACKAGE_COST = 5;

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Check if the user has enough gold
                if (user.Gold >= PACKAGE_COST)
                {
                    return false; // Not enough money
                }

                int packageId = -1;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                SELECT p.packageid
                FROM packages p
                JOIN cards c ON p.packageid = c.package_id
                WHERE c.userid IS NULL
                GROUP BY p.packageid
                HAVING COUNT(c.cardid) = 5
                LIMIT 1;
            ";

                    object? result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        transaction.Rollback();
                        return false; // No available packages
                    }
                    packageId = Convert.ToInt32(result);
                }


                int updatedGold = user.Gold - PACKAGE_COST;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                UPDATE users
                SET gold = @gold
                WHERE userid = @userid
            ";

                    AddParameter(cmd, "@gold", DbType.Int32, updatedGold);
                    AddParameter(cmd, "@userid", DbType.Int32, user.UserId);

                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                // Assign package to user
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                UPDATE cards
                SET userid = @userid
                WHERE package_id = @package_id
            ";
                    AddParameter(cmd, "@userid", DbType.Int32, user.UserId);
                    AddParameter(cmd, "@package_id", DbType.Int32, packageId);

                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                // Only Update after succesful aquire
                user.Gold = updatedGold;

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public List<Trade> GetAllTrades()
        {
            var trades = new List<Trade>();

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT tradeid, seller, cardid, type, minimumdamage 
        FROM trades";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                trades.Add(new Trade
                {
                    TradeId = reader.GetString(0),
                    Seller = reader.GetString(1),
                    CardToTrade = reader.GetString(2),
                    tradeType = reader.GetInt32(3),  // Reads type as an integer
                    MinimumDamage = reader.GetInt32(4)
                });
            }

            return trades;
        }


        public bool CreateTrade(string username, Trade trade)
        {
            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();
    

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO trades (seller, cardid, type, minimumdamage)
                VALUES (@seller, @cardid, @type, @minimumdamage)";

            AddParameter(cmd, "@seller", DbType.String, username);
            AddParameter(cmd, "@cardid", DbType.String, trade.CardToTrade);
            AddParameter(cmd, "@type", DbType.Int32, (int)trade.tradeType);
            AddParameter(cmd, "@minimumdamage", DbType.Int32, trade.MinimumDamage);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ExecuteTrade(string buyerUsername, string tradeId, string offeredCardId)
        {
            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
               
                using var tradeCmd = connection.CreateCommand();
                tradeCmd.Transaction = transaction;
                tradeCmd.CommandText = @"
                    SELECT seller, cardid, type, minimumdamage 
                    FROM trades 
                    WHERE tradeid = @tradeid";

                AddParameter(tradeCmd, "@tradeid", DbType.String, tradeId);

                using var reader = tradeCmd.ExecuteReader();
                if (!reader.Read())
                {
                    transaction.Rollback();
                    return false; 
                }

                string seller = reader.GetString(0);
                string cardToTrade = reader.GetString(1);
                string type = reader.GetString(2);
                int minDamage = reader.GetInt32(3);
                reader.Close();

                if (seller == buyerUsername)
                {
                    transaction.Rollback();
                    return false; // Can't trade with yourself
                }

                // Fetch offered card details
                using var cardCmd = connection.CreateCommand();
                cardCmd.Transaction = transaction;
                cardCmd.CommandText = @"
                    SELECT damage, card_type 
                    FROM cards 
                    WHERE cardid = @offeredCardId AND userid = (SELECT userid FROM users WHERE username = @buyer)";

                AddParameter(cardCmd, "@offeredCardId", DbType.String, offeredCardId);
                AddParameter(cardCmd, "@buyer", DbType.String, buyerUsername);

                using var cardReader = cardCmd.ExecuteReader();
                if (!cardReader.Read())
                {
                    transaction.Rollback();
                    return false; // Offered card not found
                }

                int offeredDamage = cardReader.GetInt32(0);
                string offeredType = cardReader.GetString(1);
                cardReader.Close();

                if (offeredDamage < minDamage || offeredType != type)
                {
                    transaction.Rollback();
                    return false; // Trade requirements not met
                }

                // Swap ownership of the cards
                using var updateCmd1 = connection.CreateCommand();
                updateCmd1.Transaction = transaction;
                updateCmd1.CommandText = @"
                    UPDATE cards SET userid = (SELECT userid FROM users WHERE username = @buyer)
                    WHERE cardid = @cardToTrade";

                AddParameter(updateCmd1, "@buyer", DbType.String, buyerUsername);
                AddParameter(updateCmd1, "@cardToTrade", DbType.String, cardToTrade);

                if (updateCmd1.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                using var updateCmd2 = connection.CreateCommand();
                updateCmd2.Transaction = transaction;
                updateCmd2.CommandText = @"
                    UPDATE cards SET userid = (SELECT userid FROM users WHERE username = @seller)
                    WHERE cardid = @offeredCardId";

                AddParameter(updateCmd2, "@seller", DbType.String, seller);
                AddParameter(updateCmd2, "@offeredCardId", DbType.String, offeredCardId);

                if (updateCmd2.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Delete trade from database
                using var deleteCmd = connection.CreateCommand();
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM trades WHERE tradeid = @tradeid";
                AddParameter(deleteCmd, "@tradeid", DbType.String, tradeId);
                deleteCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool DeleteTrade(string username, string tradeId)
        {
            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM trades 
                WHERE tradeid = @tradeid AND seller = @username";

            AddParameter(cmd, "@tradeid", DbType.String, tradeId);
            AddParameter(cmd, "@username", DbType.String, username);

            return cmd.ExecuteNonQuery() > 0;
        }

        private void AddParameter(IDbCommand cmd, string name, DbType type, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.DbType = type;
            param.Value = value;
            cmd.Parameters.Add(param);
        }
    }
}