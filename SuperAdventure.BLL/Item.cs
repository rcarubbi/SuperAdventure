namespace SuperAdventure.BLL
{
    public class Item
    {
        public Item(int id, string name, string namePlural, int price)
        {
            Id = id;
            Name = name;
            NamePlural = namePlural;
            Price = price;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string NamePlural { get; set; }

        public int Price { get; set; }
    }
}
