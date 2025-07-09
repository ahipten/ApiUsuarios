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

        public float? HumedadSuelo { get; set; }
        public float? Temperatura { get; set; }
        public float? Precipitacion { get; set; }
        public float? Viento { get; set; }
        public float? RadiacionSolar { get; set; }

        public string EtapaCultivo { get; set; } = "";
        public bool? NecesitaRiego { get; set; }
    }
}
