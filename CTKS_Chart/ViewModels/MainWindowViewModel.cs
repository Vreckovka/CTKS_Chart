using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;

namespace CTKS_Chart.ViewModels
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    public MainWindowViewModel(IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
   
    }

    }
}

