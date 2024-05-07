using KMeans;

namespace CTKS_Chart.Trading
{
  public class CtksIntersection
  {
    public decimal Value { get; set; }
    public TimeFrame TimeFrame { get; set; }
    public CtksLine Line { get; set; }

    public Cluster Cluster { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool IsCluster {
      get {

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
      else if(Cluster != null && other.Cluster != null)
      {
        return Cluster.Centroid.Components.Length > 1 && other.Cluster.Centroid.Components.Length > 1 &&
          Cluster.Centroid.Components[0] == other.Cluster.Centroid.Components[0] &&
          Cluster.Centroid.Components[1] == other.Cluster.Centroid.Components[1];
      }
      else
        return Value == other.Value && TimeFrame == other.TimeFrame;
    }
  }
}
