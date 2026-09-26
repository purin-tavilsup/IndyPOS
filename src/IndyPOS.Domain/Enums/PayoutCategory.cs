using System.Text.Json.Serialization;

namespace IndyPOS.Domain.Enums;

/// <summary>
/// Which side of the store a cash payout belongs to, mirroring how sales are already split.
/// Serialized by name ("Hardware", not 1) so the API and the cloud copy read without a code table.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PayoutCategory>))]
public enum PayoutCategory
{
    General,
    Hardware
}
