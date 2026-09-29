using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Shared.Hosting;

public sealed class PollingJobOptions
{
    public const string Section = "BackgroundJobs";

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 50;
}
