using Microsoft.VisualBasic;
using System.Xml.Linq;

namespace Zelenay_MTCG.Models.Cards
{
    public enum enumElementType
    {
        Fire,
        Water,
        Normal
    }

    public enum enumCardType
    {
        NotDefined = 0,
        Monster = 1,
        Spell = 2
    }

    public class Card
    {
        // The UUID from the JSON: "Id"
        public string Id { get; set; }

        // Name from JSON: "Name"
        public string Name { get; set; }

        // Damage from JSON: "Damage"
        public float Damage { get; set; }

        
        public enumElementType ElementType { get; set; }
        public enumCardType CardType { get; set; }

        // Parameterless constructor for JSON deserialization
        public Card() { }

        
        public Card(string cardId, enumElementType elementType, string cardName, float baseDamage, enumCardType cardType = enumCardType.NotDefined)
        {
            Id = cardId;
            ElementType = elementType;
            Name = cardName;
            Damage = baseDamage;
            CardType = cardType;
        }
    }
  
    }

