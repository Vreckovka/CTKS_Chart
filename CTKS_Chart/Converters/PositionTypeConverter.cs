using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VCore.WPF.Converters;

namespace CTKS_Chart.Converters
{
  public class PositionTypeConverter : BaseConverter
  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool boolValue)
        return boolValue ? "A" : "M";

      return value;
    }
  }
}
