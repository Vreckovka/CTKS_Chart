using VCore.Standard;

namespace CouldComputingServer
{
  public class SymbolToTest : ViewModel
  {
    public string Name { get; set; }

    #region IsEnabled

    private bool isEnabled = true;

    public bool IsEnabled
    {
      get { return isEnabled; }
      set
      {
        if (value != isEnabled)
        {
          isEnabled = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion



  }

}
