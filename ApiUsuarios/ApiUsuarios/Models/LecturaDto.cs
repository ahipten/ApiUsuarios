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
    }
}