using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Globalization;

namespace Models
{
    public class CustomDateTimeConverter : DateTimeConverter
    {
        private static readonly string[] formatos =
        {
            "d/M/yyyy",
            "dd/MM/yyyy",
            "d/MM/yyyy",
            "dd/M/yyyy",
            "yyyy-MM-dd"
        };

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;  // ✅ retornamos null explícito, ya no warning

            if (DateTime.TryParseExact(text.Trim(), formatos, new CultureInfo("es-PE"),
                DateTimeStyles.None, out DateTime fecha))
            {
                return fecha;
            }

            throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"No se pudo convertir '{text}' a DateTime con los formatos esperados.");
        }
    }
}
