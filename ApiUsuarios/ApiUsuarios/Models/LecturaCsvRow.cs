using CsvHelper.Configuration.Attributes;   // 👈 necesario aquí
using Helpers;                              // 👈 donde creaste el conversor

namespace Models
{
    public class LecturaCsvRow
    {
        public int? SensorId { get; set; }
        public string? Cultivo { get; set; }

        [TypeConverter(typeof(CustomDateConverter))]
        public DateTime Fecha { get; set; }
        public float HumedadSuelo { get; set; }
        public float Temperatura { get; set; }
        public float Precipitacion { get; set; }
        public float Viento { get; set; }
        public float RadiacionSolar { get; set; }
        public string? EtapaCultivo { get; set; }
        [TypeConverter(typeof(BoolFromIntConverter))]
        public bool NecesitaRiego { get; set; }

        // 🔹 Nuevos campos
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double IndiceSequia { get; set; }
        public double MateriaOrganica { get; set; }
        public string MetodoRiego { get; set; } = "";
        public double pH_Suelo { get; set; }
        public float IndiceEstres { get; set; }
        public float DeficitHidrico { get; set; }
        public float Evapotranspiracion { get; set; }

    }

}
