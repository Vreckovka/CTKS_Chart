using System.Collections.ObjectModel;
using VCore.Standard;

namespace TradingManager.ViewModels
{
  public class TradingViewFolderDataViewModel : ViewModel
  {
    #region Name

    private string name;

    public string Name
    {
      get { return name; }
      set
      {
        if (value != name)
        {
          name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Path

    private string path;

    public string Path
    {
      get { return path; }
      set
      {
        if (value != path)
        {
          path = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public ObservableCollection<TradingViewDataViewModel> Files { get; set; } = new ObservableCollection<TradingViewDataViewModel>();
  }
}
