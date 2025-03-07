using Zelenay_MTCG.Models.Cards;
namespace Zelenay_MTCG.Models.TradeModel
{
    public class Trade
    {
        public string TradeId { get; set; }
        public string Seller { get; set; }
        public string CardToTrade { get; set; }
        public int tradeType { get; set; }
        public int MinimumDamage { get; set; }

        public Trade() { }

        public Trade(string tradeId, string seller, string cardToTrade, int cardType, int minimumDamage)
        {
            TradeId = tradeId;
            Seller = seller;
            CardToTrade = cardToTrade;
            tradeType = cardType;
            MinimumDamage = minimumDamage;
        }
    }
}
