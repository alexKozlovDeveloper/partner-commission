using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Contracts;

public sealed record CommissionPaymentsQuery(
    [Required] IReadOnlyList<Guid> CommissionIds
    );
