﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zelenay_MTCG.Models.Cards;
namespace Zelenay_MTCG.Models.Usermodel
{
    
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int Gold { get; set; }
        public int Elo { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public string AuthToken { get; set; }
        public int UserId { get; set; }

        public string Name { get; set; }
        public string Bio { get; set; }
        public string Image { get; set; }
        public User()
        {

        }
    }
}
