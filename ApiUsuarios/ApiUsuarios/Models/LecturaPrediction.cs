using Microsoft.ML.Data;
namespace Models
{
    public class LecturaPrediction
{
    [ColumnName("PredictedLabel")]
    public bool NecesitaRiego { get; set; }

    public float[]? Score { get; set; }

    public float Probability => Score?.Length > 1 ? Score[1] : 0f;
}

}
