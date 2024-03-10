using System;
using System.Windows.Input;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class SimulationPromptViewModel : BasePromptViewModel<SimulationTradingBot>
  {

    public SimulationPromptViewModel(SimulationTradingBot model) : base(model)
    {
    }

    #region StartCommand

    protected ActionCommand startCommand;

    public ICommand StartCommand
    {
      get
      {
        return startCommand ??= new ActionCommand(OnStart);
      }
    }

    public void OnStart()
    {
      this.Model.Start();
    }

    #endregion

   
  }
}