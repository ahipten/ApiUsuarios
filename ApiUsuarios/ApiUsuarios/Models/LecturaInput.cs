namespace Models
{
    /// <summary>Entrada para predicción de riego.</summary>
public class LecturaInput
{
    /// <summary>Humedad del suelo (%)</summary>
    public float HumedadSuelo { get; set; }

    /// <summary>Temperatura ambiente (°C)</summary>
    public float Temperatura { get; set; }

    /// <summary>Precipitación (mm)</summary>
    public float Precipitacion { get; set; }

    /// <summary>Velocidad del viento (km/h)</summary>
    public float Viento { get; set; }

    /// <summary>Radiación solar (W/m²)</summary>
    public float RadiacionSolar { get; set; }

    /// <summary>Etapa del cultivo (ej. Siembra, Floración)</summary>
    public string? EtapaCultivo { get; set; }

    /// <summary>Nombre del cultivo (ej. Maíz, Palta)</summary>
    public string? Cultivo { get; set; }

    /// <summary>Fecha de la lectura</summary>
    public DateTime Fecha { get; set; }
}


}