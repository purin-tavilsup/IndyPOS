namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Store feature flags exposed to clients (WinForms) so store-type gating can be
/// applied in the UI. Mirrors <c>IndyPOS.Domain.ValueObjects.StoreTypeFeatures</c>.
/// </summary>
public record StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled);
