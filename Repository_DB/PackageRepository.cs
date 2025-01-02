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

namespace Zelenay_MTCG.Repository_DB
{
    public class PackageRepository
    {
        private readonly string dbConnString;
        private readonly DBconn DBcs;

        public PackageRepository()
        {
            dbConnString = "Host=localhost;Database=mydb;Username=user;Password=password";
            DBcs = new DBconn(dbConnString);
        }

        public void CreatePackage(List<Card> cards)
        {
            using IDbConnection connection = DBcs.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1) Create a new package row
                int newPackageId;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        INSERT INTO packages (created_at)
                        VALUES (NOW())
                        RETURNING packageid;
                    ";

                    newPackageId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2) Insert each card referencing 'newPackageId'
                foreach (var card in cards)
                {
                    using var cardCmd = connection.CreateCommand();
                    cardCmd.Transaction = transaction;
                    cardCmd.CommandText = @"
                        INSERT INTO cards (cardid, name, damage, packageid)
                        VALUES (@cardid, @name, @damage, @packageid);
                    ";

                    AddParameter(cardCmd, "@cardid", DbType.String, card.Id.ToString());
                    AddParameter(cardCmd, "@name", DbType.String, card.Name);
                    AddParameter(cardCmd, "@damage", DbType.Decimal, card.Damage);
                    AddParameter(cardCmd, "@packageid", DbType.Int32, newPackageId);
                    cardCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
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
