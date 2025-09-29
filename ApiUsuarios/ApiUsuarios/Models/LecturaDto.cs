namespace Models
{
    public class LecturaDto
    {
        public int SensorId { get; set; }
        public int CultivoId { get; set; }  // opcional si Cultivo se envía como texto
        public string? Cultivo { get; set; }  // ej. "Maíz"
        public DateTime Fecha { get; set; }
        public float HumedadSuelo { get; set; }
        public float Temperatura { get; set; }
        public float Precipitacion { get; set; }
        public float Viento { get; set; }
        public float RadiacionSolar { get; set; }
        public string? EtapaCultivo { get; set; }
        public bool? NecesitaRiego { get; set; }

            // 🔹 Nuevos campos
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double IndiceSequia { get; set; }
        public double MateriaOrganica { get; set; }
        public string MetodoRiego { get; set; } = "";
        public double pH_Suelo { get; set; }
    }
}