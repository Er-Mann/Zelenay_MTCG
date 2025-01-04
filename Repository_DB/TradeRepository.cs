using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Zelenay_MTCG.Models.Usermodel;

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
            // Example cost for 1 package
            const int PACKAGE_COST = 5;

            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1) Check user’s gold
                if (user.Gold < PACKAGE_COST)
                {
                    // Not enough money => fail
                    return false;
                }

                // 2) Find a package that is “unsold.” 
                //    We can consider “unsold” if all its cards have no user assigned,
                //    or if we have a packages table with a 'sold' flag, etc.
                //    For simplicity, let's find the first package whose cards are not assigned:
                int packageId = -1;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    // One approach: find any package that has all 5 cards with userId = NULL
                    // This is a simplified example (adjust to your schema)
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
                        // No available package => fail
                        return false;
                    }
                    packageId = Convert.ToInt32(result);
                }

                // 3) Subtract cost from user
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
                    cmd.ExecuteNonQuery();
                }
                user.Gold = updatedGold;

                // 4) Assign all cards in that package to user
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
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                // Optionally update user object in memory
                // or reload user from DB:
                // user = _userRepo.GetUserById(user.UserId);

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
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