using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace Helpers
{
    public class CustomDateConverter : DefaultTypeConverter
    {
        private static readonly string[] formatos = new[]
        {
            "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy"
        };

        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (DateTime.TryParseExact(text, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                return fecha;

            throw new TypeConverterException(this, memberMapData, text, row.Context, $"Fecha inválida: '{text}'");
        }
    }
}
