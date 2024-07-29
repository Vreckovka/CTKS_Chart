namespace CTKS_Chart.ViewModels
{
  public interface ISimulationTradingBot : ITradingBotViewModel
  {
    public void LoadSimulationResults();

    public bool SaveResults { get; set; }
  }
}
