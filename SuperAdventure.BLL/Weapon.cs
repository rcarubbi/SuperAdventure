namespace SuperAdventure.BLL
{
    public class Weapon : Item
    {
        public Weapon(int id, string name, string namePlural, int minimumDamage, int maximumDamage, int price) 
            : base(id, name, namePlural, price)
        {
            MinimumDamage = minimumDamage;
            MaximumDamage = maximumDamage;
        }

        public int MinimumDamage { get; set; }
        public int MaximumDamage { get; set; }
    }
}
