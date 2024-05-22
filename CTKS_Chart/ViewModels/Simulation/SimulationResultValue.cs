using CTKS_Chart.Trading;

namespace CTKS_Chart.ViewModels
{
  public class SimulationResultValue
  {
    public SimulationResultValue()
    {

    }

    public SimulationResultValue(decimal value)
    {
      Value = value;
    }

    public decimal Value { get; set; }
    public Candle Candle { get; set; }
  }
}
