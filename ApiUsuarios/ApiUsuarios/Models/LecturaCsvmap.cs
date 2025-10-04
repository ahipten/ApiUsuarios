using CsvHelper.Configuration;
namespace Models
{
    // Mapea las columnas del CSV a las propiedades de LecturaCsvRow
    // Asegúrate de que los nombres coincidan con los encabezados del CSV
    public sealed class LecturaCsvMap : ClassMap<LecturaCsvRow>
    {
        public LecturaCsvMap()
        {
            // Si el CSV trae un encabezado "SensorId" lo mapea directo
            Map(m => m.SensorId).Name("SensorId");

            // Cultivo puede venir como "Cultivo"
            Map(m => m.Cultivo).Name("Cultivo");

            Map(m => m.Fecha).Name("Fecha")
            .TypeConverter<CustomDateTimeConverter>();
            Map(m => m.HumedadSuelo).Name("HumedadSuelo");
            Map(m => m.Temperatura).Name("Temperatura");
            Map(m => m.Precipitacion).Name("Precipitacion");
            Map(m => m.Viento).Name("Viento");
            Map(m => m.RadiacionSolar).Name("RadiacionSolar");
            Map(m => m.EtapaCultivo).Name("EtapaCultivo");
            Map(m => m.NecesitaRiego).Name("NecesitaRiego");
            Map(m => m.IndiceSequia).Name("IndiceSequia");
            Map(m => m.MateriaOrganica).Name("MateriaOrganica");
            Map(m => m.MetodoRiego).Name("MetodoRiego");
            Map(m => m.pH_Suelo).Name("pH_Suelo");
            Map(m => m.IndiceEstres).Name("IndiceEstres");
            Map(m => m.DeficitHidrico).Name("DeficitHidrico");
            Map(m => m.Evapotranspiracion).Name("Evapotranspiracion");

            // 🔑 Aquí la corrección importante
            Map(m => m.Lat).Name("Latitud");
            Map(m => m.Lng).Name("Longitud");
        }
    }
}