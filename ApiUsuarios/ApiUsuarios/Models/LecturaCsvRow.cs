using CsvHelper.Configuration.Attributes;   // 👈 necesario aquí
using Helpers;                              // 👈 donde creaste el conversor

namespace Models
{
    public class LecturaCsvRow
    {
        public int? SensorId { get; set; }
        public string? Cultivo { get; set; }
        public DateTime Fecha { get; set; }
        public float HumedadSuelo { get; set; }
        public float Temperatura { get; set; }
        public float Precipitacion { get; set; }
        public float Viento { get; set; }
        public float RadiacionSolar { get; set; }
        public string? EtapaCultivo { get; set; }
        [TypeConverter(typeof(BoolFromIntConverter))]
        public bool NecesitaRiego { get; set; }
    }
}