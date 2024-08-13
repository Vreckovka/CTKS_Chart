namespace CloudComputing.Domains
{
  public class ServerAdress
  {
    public string IP { get; set; }
    public int Port { get; set; }
  }

  public static class MessageContract
  {
    public const string Done = "*";
    public const string EndOfMessage = "|";
  }
}
