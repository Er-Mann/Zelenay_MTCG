﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonsterCardGame.Models.Cards;
namespace MonsterCardGame.Models.User
{
    
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int Gold { get; set; }
        public List<Card> CardStack { get; set; } //alle karten des users
        public List<Card> BattleDeck { get; set; }    //4 karten die zum kämpfen benutzt werden
        public int Elo { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public string AuthToken { get; set; }
        public int UserId { get; set; }
        public User()
        {

        }
        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Gold = 20;
            CardStack = new List<Card>();
            BattleDeck = new List<Card>();
            Elo = 100;
            Losses = 0;
            Wins = 0;
            
        }
    }
}