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
        // Primary key in DB if you need it


        // The UUID from the JSON: "Id"
        public string Id { get; set; }

        // Name from JSON: "Name"
        public string Name { get; set; }

        // Damage from JSON: "Damage"
        public float Damage { get; set; }

        // The numeric enums (not in JSON, but we infer from Name)
        public enumElementType ElementType { get; set; }
        public enumCardType CardType { get; set; }

        // Parameterless constructor for JSON deserialization
        public Card() { }

        // (Optional) Internal or private constructor if you still need it in code
        internal Card(string cardId, enumElementType elementType, string cardName, float baseDamage, enumCardType cardType = enumCardType.NotDefined)
        {
            Id = cardId;
            ElementType = elementType;
            Name = cardName;
            Damage = baseDamage;
            CardType = cardType;
        }

        public void InferTypesFromName()
        {
            // If "Spell" in Name => CardType.Spell; otherwise Monster
            if (Name?.Contains("Spell", StringComparison.OrdinalIgnoreCase) == true)
                CardType = enumCardType.Spell;
            else
                CardType = enumCardType.Monster;

            // If "Fire" in Name => ElementType.Fire; "Water" => Water; else Normal
            if (Name?.Contains("Fire", StringComparison.OrdinalIgnoreCase) == true)
                ElementType = enumElementType.Fire;
            else if (Name?.Contains("Water", StringComparison.OrdinalIgnoreCase) == true)
                ElementType = enumElementType.Water;
            else
                ElementType = enumElementType.Normal;
        }
    }
}

//public enum enumName
//{
//    Dragon,
//    Elf,
//    Goblin,
//    Knight,
//    Kraken,
//    Ork,
//    Wizard,
//
