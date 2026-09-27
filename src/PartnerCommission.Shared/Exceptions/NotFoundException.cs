namespace PartnerCommission.Shared.Exceptions;

public sealed class NotFoundException(string resource, string key)
    : Exception($"{resource} '{key}' not found");
