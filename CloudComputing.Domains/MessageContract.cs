using System.Text.RegularExpressions;

namespace CloudComputing.Domains
{
  public static class MessageContract
  {
    public static string Done { get; } = "1DONE1";
    public static string StartOfMessage { get; } = "1START1";
    public static string EndOfMessage { get; } = "1END1";
    public static string Error  { get; } = "1ERROR1";

    public static int BUFFER_SIZE_CLIENT { get; } = 65536;

    private static Regex messageRegex = new Regex($"{StartOfMessage}(.*?){EndOfMessage}");
    public static string GetDataMessageContent(string data)
    {
      var v = messageRegex.Match(data);
      return v.Groups[1].ToString();
    }

    public static bool IsDataMessage(string data)
    {
      return messageRegex.IsMatch(data);
    }

    public static string GetDataMessage(string data)
    {
      return StartOfMessage + data + EndOfMessage;
    }
  }
}
