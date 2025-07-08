using Microsoft.ML.Data;
namespace Models
{
    public class LecturaPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool NecesitaRiego { get; set; }
        public float[] Score { get; set; }
    }

}
