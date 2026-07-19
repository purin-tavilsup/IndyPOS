using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

internal static class PaymentMethodMappingExtensions
{
    public static PaymentMethodDto ToDto(this PaymentMethod method) =>
        new(method.Code, method.DisplayName, method.Kind, method.IsEnabled, method.DisplayOrder);

    public static IReadOnlyList<PaymentMethodDto> ToDtos(this IEnumerable<PaymentMethod> methods) =>
        methods.Select(ToDto).ToList();
}
