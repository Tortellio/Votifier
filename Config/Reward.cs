namespace fr34kyn01535.Votifier.Config
{
    public class Reward
    {
        public Reward() { }

        public Reward(ushort itemId, byte amount)
        {
            ItemId = itemId;
            Amount = amount;
        }

        public ushort ItemId { get; set; }
        public byte Amount { get; set; }
    }
}