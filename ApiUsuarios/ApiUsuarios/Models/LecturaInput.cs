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

    // Este campo no se usa en el modelo, solo en el controlador
    public DateTime Fecha { get; set; } = DateTime.Now;

    // Label para entrenamiento
    public bool NecesitaRiego { get; set; }
}


}
