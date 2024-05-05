using System;
using System.Reactive.Linq;
using System.Windows;
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

    public override string Title { get; set; } = "Simulation";

  
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

    #region StopCommand

    protected ActionCommand stopCommand;

    public ICommand StopCommand
    {
      get
      {
        return stopCommand ??= new ActionCommand(OnStop);
      }
    }

    public void OnStop()
    {
      this.Model.Stop();
    }

    #endregion

    protected override void OnClose(Window window)
    { 
      base.OnClose(window);

      this.Model.Stop();
    }
  }
}