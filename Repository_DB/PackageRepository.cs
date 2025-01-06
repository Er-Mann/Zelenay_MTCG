using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zelenay_MTCG.Server.Endpoints.Packageendpoint;
using Zelenay_MTCG.Models.Cards;
using Zelenay_MTCG.Models.Package;
using Zelenay_MTCG.Server.HttpModel;
using System.Transactions;

namespace Zelenay_MTCG.Repository_DB
{
        public class PackageRepository
        {
            private readonly DBconn _dbConn;

        public PackageRepository()
        {
            _dbConn = new DBconn();
        }

        // private readonly CardRepository _cardRepository;



        public void CreatePackage(List<Card> cards)
        {


            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1) Insert a row in packages
                int newPackageId;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        INSERT INTO mydb.public.packages
                        DEFAULT VALUES
                        RETURNING packageid;
                    ";

                    newPackageId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2) Insert each card referencing newPackageId
                foreach (var card in cards)
                {
                    // We can reuse the same connection if we want, just need the same transaction:
                    using var cmd2 = connection.CreateCommand();
                    cmd2.Transaction = transaction;

                    cmd2.CommandText = @"
                        INSERT INTO mydb.public.cards
                            (cardid, name, damage, element_type, card_type, package_id)
                        VALUES
                            (@cardid, @name, @damage, @element_type, @card_type, @package_id)
                        RETURNING cardid;
    ";

                    AddParameter(cmd2, "@cardid", DbType.String, card.Id ?? (object)DBNull.Value);
                    AddParameter(cmd2, "@name", DbType.String, card.Name);
                    AddParameter(cmd2, "@damage", DbType.Decimal, card.Damage);
                    AddParameter(cmd2, "@element_type", DbType.Int32, (int)card.ElementType);
                    AddParameter(cmd2, "@card_type", DbType.Int32, (int)card.CardType);
                    AddParameter(cmd2, "@package_id", DbType.Int32, newPackageId);

                    // The database will return the same ID you just inserted:
                    card.Id = cmd2.ExecuteScalar().ToString();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public bool CheckUniqueCardIds(List<Card> cards)
        {
            using IDbConnection connection = _dbConn.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            foreach (var card in cards)
            {
                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.Transaction = transaction;
                    checkCmd.CommandText = @"
                        SELECT COUNT(*)
                        FROM mydb.public.cards
                        WHERE cardid = @cardid";

                    AddParameter(checkCmd, "@cardid", DbType.String, card.Id);

                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        return true;
                    }
                  
                }
            }  
            return false;
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
