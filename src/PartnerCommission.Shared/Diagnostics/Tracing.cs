using System.Diagnostics;

namespace PartnerCommission.Shared.Diagnostics;

public static class Tracing
{
    public const string SourceName = "PartnerCommission";

    public static readonly ActivitySource Source = new(SourceName);

    private static readonly ActivityListener Listener = new()
    {
        ShouldListenTo = source => source.Name == SourceName,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
    };

    private static int _listenerRegistered;

    public static void EnsureListenerRegistered()
    {
        if (Interlocked.Exchange(ref _listenerRegistered, 1) == 0)
            ActivitySource.AddActivityListener(Listener);
    }
}
