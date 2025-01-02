using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Zelenay_MTCG.Repository_DB
{
    public class DBconn
    {
        private readonly string _dbConnString;

        public DBconn(string dbConnString)
        {
            _dbConnString = dbConnString ??
                throw new ArgumentNullException(nameof(dbConnString));
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_dbConnString);
        }
        public void CreateTables()
        {
            using IDbConnection connection = new NpgsqlConnection(_dbConnString);
            connection.Open();

            // Create a single command that executes multiple CREATE TABLE statements
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
            -- USERS table (matches UserRepository: 'userid', 'username', 'password', etc.)
            CREATE TABLE IF NOT EXISTS mydb.public.users (
                userid         SERIAL PRIMARY KEY,
                username       TEXT    NOT NULL UNIQUE,
                password       TEXT    NOT NULL,
                elo            INT     NOT NULL DEFAULT 100,
                gold           INT     NOT NULL DEFAULT 20,
                wins           INT     NOT NULL DEFAULT 0,
                losses         INT     NOT NULL DEFAULT 0,
                authtoken      TEXT
            );

            -- CARDS table, referencing 'userid' (FK) in 'users' table
            CREATE TABLE IF NOT EXISTS mydb.public.cards (
                cardid        SERIAL PRIMARY KEY,
                name          TEXT    NOT NULL,
                element_type  INT     NOT NULL,
                damage        DECIMAL NOT NULL,
                card_type     INT     NOT NULL,
                package_id    INT,         -- If/when you implement 'packages', you can link here
                userid        INT,
                FOREIGN KEY (userid) REFERENCES mydb.public.users (userid)
            );

            -- DECKS table, referencing 'userid' (FK) in 'users' table
            -- plus references to 4 cards
            CREATE TABLE IF NOT EXISTS mydb.public.decks (
                deckid     SERIAL PRIMARY KEY,
                userid     INT NOT NULL,
                card_1_id  INT NOT NULL,
                card_2_id  INT NOT NULL,
                card_3_id  INT NOT NULL,
                card_4_id  INT NOT NULL,
                FOREIGN KEY (userid)    REFERENCES mydb.public.users (userid),
                FOREIGN KEY (card_1_id) REFERENCES mydb.public.cards (cardid),
                FOREIGN KEY (card_2_id) REFERENCES mydb.public.cards (cardid),
                FOREIGN KEY (card_3_id) REFERENCES mydb.public.cards (cardid),
                FOREIGN KEY (card_4_id) REFERENCES mydb.public.cards (cardid)
            );

            -- TRADES table, referencing 'userid' (FK) in 'users' and 'cardid' in 'cards'
            CREATE TABLE IF NOT EXISTS mydb.public.trades (
                tradeid        SERIAL PRIMARY KEY,
                userid         INT     NOT NULL,
                cardid         INT     NOT NULL,
                card_type      INT     NOT NULL,
                minimum_damage DECIMAL NOT NULL,
                FOREIGN KEY (userid) REFERENCES mydb.public.users (userid),
                FOREIGN KEY (cardid) REFERENCES mydb.public.cards (cardid)
            );

            -- BATTLES table, referencing 2 players (userid) and storing a winner (userid)
            CREATE TABLE IF NOT EXISTS mydb.public.battles (
                battleid     SERIAL PRIMARY KEY,
                player_1_id  INT NOT NULL,
                player_2_id  INT NOT NULL,
                winner_id    INT,
                FOREIGN KEY (player_1_id) REFERENCES mydb.public.users (userid),
                FOREIGN KEY (player_2_id) REFERENCES mydb.public.users (userid),
                FOREIGN KEY (winner_id)   REFERENCES mydb.public.users (userid)
            );
        ";

            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}

