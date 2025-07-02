namespace Models
{
    public class Cultivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public ICollection<Lectura>? Lecturas { get; set; }
    }
}
