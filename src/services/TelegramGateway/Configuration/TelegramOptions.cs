namespace TelegramGateway.Configuration;

public class TelegramOptions
{
    public const string Section = "Telegram";

    public int    ApiId        { get; set; }
    public string ApiHash      { get; set; } = string.Empty;
    public string AccountPhone { get; set; } = string.Empty;

    // Path where WTelegramClient stores the session file.
    // Must be on a persistent volume in Kubernetes.
    public string SessionPath  { get; set; } = "/data/session.session";

    // Run in interactive auth mode — needed only on first run to enter OTP.
    // Set TELEGRAM__SETUPMODE=true, run interactively, then redeploy normally.
    public bool SetupMode { get; set; }
}
