namespace BazaarBot.Engine
{
    public class Commodity
    {
        public string id;
        public float size = 1.0f;	//inventory size taken up

        public Commodity() { }

        public Commodity(string _id, float _size)
        {
            id = _id;
            size = _size;
        }

        public Commodity Copy()
        {
            return new Commodity(id, size);
        }
    }
}