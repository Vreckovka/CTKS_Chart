﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.Standard.Modularity.Interfaces;

namespace TradingManager.Views
{
  /// <summary>
  /// Interaction logic for ChartView.xaml
  /// </summary>
  public partial class FileDetailView : UserControl, IView
  {
    public FileDetailView()
    {
      InitializeComponent();
    }
  }
}
