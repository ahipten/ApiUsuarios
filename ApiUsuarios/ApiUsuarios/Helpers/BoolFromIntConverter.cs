using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Helpers
{
    // Convierte "1" → true, "0" → false, y delega a BooleanConverter para otros valores
    public class BoolFromIntConverter : BooleanConverter
    {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text == null)
            return false;

        var trimmed = text.Trim();
        return trimmed switch
        {
            "1" => true,
            "0" => false,
            _   => base.ConvertFromString(trimmed, row, memberMapData) ?? false
        };
    }

    }
}
