namespace PartnerCommission.Shared.Hosting;

public static class Retry
{
    public const int MaxErrorLength = 1000;

    private const double MaxDelaySeconds = 300;

    public static TimeSpan NextDelay(int attempts)
    {
        var result = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempts), MaxDelaySeconds));

        return result;
    }

    public static string ErrorText(Exception ex)
    {
        var result = ex.Message.Length > MaxErrorLength ? ex.Message[..MaxErrorLength] : ex.Message;

        return result;
    }
}
