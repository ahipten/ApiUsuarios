using System;
namespace Models
{
    public class LecturaInput
    {

        public float HumedadSuelo { get; set; }
        public float Temperatura { get; set; }
        public float Precipitacion { get; set; }
        public float Viento { get; set; }
        public float RadiacionSolar { get; set; }
        public string EtapaCultivo { get; set; } = string.Empty;
        public string Cultivo { get; set; } = string.Empty;
        public bool NecesitaRiego { get; set; }

    }
}
