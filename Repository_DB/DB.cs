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

        public DBconn()
        {
            _dbConnString = "Host=localhost;Database=mydb;Username=user;Password=password";
           
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_dbConnString);
        }

        public void CreateTables()
        {
            using IDbConnection connection = new NpgsqlConnection(_dbConnString);
            connection.Open();

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
        -- 1) Drop all existing tables (for clean testing)
        DROP TABLE IF EXISTS battles   CASCADE;
        DROP TABLE IF EXISTS trades    CASCADE;
        DROP TABLE IF EXISTS decks     CASCADE;
        DROP TABLE IF EXISTS cards     CASCADE;
        DROP TABLE IF EXISTS packages  CASCADE;
        DROP TABLE IF EXISTS users     CASCADE;

        -- 2) USERS table
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

        -- 3) PACKAGES table
        CREATE TABLE IF NOT EXISTS mydb.public.packages (
            packageid   SERIAL PRIMARY KEY
        );

       
        CREATE TABLE IF NOT EXISTS mydb.public.cards (
            cardid       TEXT PRIMARY KEY,
            name         TEXT    NOT NULL,
            element_type INT     NOT NULL,
            damage       DECIMAL NOT NULL,
            card_type    INT     NOT NULL,
            package_id   INT,
            userid       INT,
            FOREIGN KEY (package_id) REFERENCES mydb.public.packages (packageid),
            FOREIGN KEY (userid)    REFERENCES mydb.public.users (userid)
);


        -- 5) DECKS table
        CREATE TABLE IF NOT EXISTS mydb.public.decks (
            deckid     SERIAL PRIMARY KEY,
            userid     INT NOT NULL,
            card_1_id  TEXT NOT NULL,
            card_2_id  TEXT NOT NULL,
            card_3_id  TEXT NOT NULL,
            card_4_id  TEXT NOT NULL,
            FOREIGN KEY (userid)    REFERENCES mydb.public.users (userid),
            FOREIGN KEY (card_1_id) REFERENCES mydb.public.cards (cardid),
            FOREIGN KEY (card_2_id) REFERENCES mydb.public.cards (cardid),
            FOREIGN KEY (card_3_id) REFERENCES mydb.public.cards (cardid),
            FOREIGN KEY (card_4_id) REFERENCES mydb.public.cards (cardid)
        );

        -- 6) TRADES table
        CREATE TABLE IF NOT EXISTS mydb.public.trades (
            tradeid        SERIAL PRIMARY KEY,
            userid         INT     NOT NULL,
            cardid         TEXT     NOT NULL,
            card_type      INT     NOT NULL,
            minimum_damage DECIMAL NOT NULL,
            FOREIGN KEY (userid) REFERENCES mydb.public.users (userid),
            FOREIGN KEY (cardid) REFERENCES mydb.public.cards (cardid)
        );

        -- 7) BATTLES table
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
            //connection.Close();
        }


    }
}

