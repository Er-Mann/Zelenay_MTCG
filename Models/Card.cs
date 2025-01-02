using System.Xml.Linq;

namespace Zelenay_MTCG.Models.Cards;

public class Card
{
    public Card(string cardId, enumElementType elementType, enumName cardName, int baseDamage)
    {
        Id = cardId;
        ElementType = elementType;
        Name = cardName;
        Damage = baseDamage;
    
    }
    
    public enumElementType ElementType { get; set; }

    public enumName Name { get; set; }

    public int Damage { get; set; }
    public string Id { get; set; }


}
public enum enumElementType
{
    Fire,
    Water,
    Normal
}
public enum enumName
{
    Dragon,
    Elf,
    Goblin,
    Knight,
    Kraken,
    Ork,
    Wizard,
}