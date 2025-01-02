

namespace MonsterCardGame.Models.Cards;

public class Card
{
    public Card(ElementType elementType, CardName cardName, int baseDamage, int cardRarity)
    {
        ElementType = elementType;
        CardName = cardName;
        BaseDamage = baseDamage;
        CardRarity = cardRarity;
    }
    
    public ElementType ElementType { get; protected set; }

    public CardName CardName { get; protected set; }
    
    public int BaseDamage { get; protected set; }
    
    public int CardRarity { get; protected set; }
    
}
public enum ElementType
{
    Fire,
    Water,
    Normal
}
public enum CardName
{
    Dragon,
    Elf,
    Goblin,
    Knight,
    Kraken,
    Ork,
    Wizard,
}