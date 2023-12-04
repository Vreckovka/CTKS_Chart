using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Other;

#pragma warning disable 618

namespace CTKS_Chart
{
  public partial class MainWindow : IView
  {
    public MainWindow()
    {
      InitializeComponent();
    }
  }
}