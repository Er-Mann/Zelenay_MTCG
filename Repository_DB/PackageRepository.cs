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
                            (name, damage, element_type, card_type, package_id)
                        VALUES
                            (@name, @damage, @element_type, @card_type, @package_id)
                        RETURNING cardid;
                    ";

                        AddParameter(cmd2, "@name", DbType.String, card.Name);
                        AddParameter(cmd2, "@damage", DbType.Decimal, card.Damage);
                        AddParameter(cmd2, "@element_type", DbType.Int32, (int)card.ElementType);   //This is Npgsql telling you
                                                                                                    //“I see you’re trying to pass an enum to a parameter that’s declared as an integer type.I can’t do that conversion automatically.You must give me an int, not an enum.” Lösung: (int)
                        AddParameter(cmd2, "@card_type", DbType.Int32, (int)card.CardType);
                        AddParameter(cmd2, "@package_id", DbType.Int32, newPackageId);

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
