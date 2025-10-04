using System;
using Microsoft.ML.Data; // 👈 necesario si usas [ColumnName]

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
        public float Mes { get; set; }
        public float DiaDelAnio { get; set; }

        // ✅ Nueva propiedad esperada por el modelo
        [ColumnName("Metodo_Riego")]
        public string Metodo_Riego { get; set; } = "Goteo"; // puedes cambiar el valor por defecto

        [ColumnName("Latitud")]
        public float Latitud { get; set; }

        [ColumnName("Longitud")]
        public float Longitud { get; set; }
        // ✅ Nueva columna obligatoria según el modelo
        [ColumnName("Indice_Sequía")]
        public float Indice_Sequía { get; set; } = 0.0f;

        [ColumnName("pH_Suelo")]
        public float pH_Suelo { get; set; } = 7.0f; // valor neutro por defecto

        [ColumnName("Materia_Organica")]
        public float Materia_Organica { get; set; } = 3.0f;

        [ColumnName("Interaccion_HT")]
        public float Interaccion_HT { get; set; }

        [ColumnName("Balance_Agua")]
        public float Balance_Agua { get; set; }

        [ColumnName("Sequía_MO")]
        public float Sequía_MO { get; set; }

        [ColumnName("IndiceEstres")]
        public float IndiceEstres { get; set; }

        [ColumnName("DeficitHidrico")]
        public float DeficitHidrico { get; set; }

        [ColumnName("Evapotranspiracion")]
        public float Evapotranspiracion { get; set; }
    }
}
