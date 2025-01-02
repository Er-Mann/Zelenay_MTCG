using Zelenay_MTCG.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zelenay_MTCG.Models.Package;

public class Package
{
   
    public int PackageId { get; set; }
    public List<Card> Cards { get; set; }
    public int Price { get; set; } 
    
    public Package(List<Card> cards)
    {
        Cards = cards;
        Price = 5;
    }
}
