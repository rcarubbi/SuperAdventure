namespace SuperAdventure.BLL
{
    public class HealingPotion : Item
    {
        public HealingPotion(int id, string name, string namePlural, int amountToHeal, int price) 
            : base(id, name, namePlural, price)
        {
            AmountToHeal = amountToHeal;
        }

        public int AmountToHeal { get; set; }
    }
}
