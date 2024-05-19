using System.Collections;
using System.Collections.Generic;

namespace CTKS_Chart.Trading
{
  public class CtksCluster
  {
    public decimal Value { get; set; }
    public IEnumerable<CtksIntersection> Intersections { get; set; }
  }

  public enum IntersectionType
  {
    CoreLine,
    Cluster,
    RangeFilter,
    RangeFilterH,
    RangeFilterL
  }


  public class CtksIntersection
  {
    public decimal Value { get; set; }
    public TimeFrame TimeFrame { get; set; }
    public CtksLine Line { get; set; }
    public CtksCluster Cluster { get; set; }
    public IntersectionType IntersectionType { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsCluster
    {
      get
      {

        return Cluster != null;
      }
    }

    public bool IsSame(CtksIntersection other)
    {
      if (Line != null && other.Line != null)
      {
        //Simulation is giving significant less value without Value == other.Value;
        var result =
          Line.IsSame(other.Line)
          && Value == other.Value;

        return result;
      }
      else if (Cluster != null && other.Cluster != null)
      {
        return Cluster.Value == other.Cluster.Value && TimeFrame == other.TimeFrame;
      }
      else if (Cluster != null && other.Cluster == null || Line != null && other.Line == null)
      {
        return false;
      }
      else
        return Value == other.Value && TimeFrame == other.TimeFrame;
    }
  }
}
