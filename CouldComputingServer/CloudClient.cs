using System.Collections.Generic;
using System.Net.Sockets;

namespace CouldComputingServer
{
  public class CloudClient
  {
    public TcpClient Client { get; set; }
    public Dictionary<uint, bool> SentBuyGenomes { get; set; } = new Dictionary<uint, bool>();
    public Dictionary<uint, bool> SentSellGenomes { get; set; } = new Dictionary<uint, bool>();

    public bool Done { get; set; }
  }

}
