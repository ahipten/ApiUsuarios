namespace Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public int UsuarioId { get; set; }
        public User? Usuario { get; set; }
        public ICollection<Lectura>? Lecturas { get; set; }
    }
}
