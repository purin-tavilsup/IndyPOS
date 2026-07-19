using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public record PaymentMethodDto(string Code, string DisplayName, PaymentMethodKind Kind, bool IsEnabled, int DisplayOrder);
