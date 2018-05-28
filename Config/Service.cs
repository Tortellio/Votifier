namespace fr34kyn01535.Votifier.Config
{
    public class Service
    {
        public Service() { }

        public Service(string name)
        {
            Name = name;
        }

        public string Name { get; set; } = "";
        public string APIKey { get; set; } = "";
    }
}