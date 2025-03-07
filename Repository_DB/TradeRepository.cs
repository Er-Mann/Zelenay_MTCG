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