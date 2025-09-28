namespace Models
{
    public class Lectura
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public Sensor? Sensor { get; set; }
        public int CultivoId { get; set; }
        public Cultivo? Cultivo { get; set; }
        public DateTime Fecha { get; set; }

        public double HumedadSuelo { get; set; }
        public double Temperatura { get; set; }
        public double Precipitacion { get; set; }
        public double Viento { get; set; }
        public double RadiacionSolar { get; set; }
            // ✅ Nuevos campos si aún no existen:
        public double? IndiceSequia { get; set; }
        public double? pH_Suelo { get; set; }
        public double? MateriaOrganica { get; set; }
        public string? MetodoRiego { get; set; }
        public string EtapaCultivo { get; set; } = "";
        public bool? NecesitaRiego { get; set; }

        // 🔽 Agrega esto
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }
}
