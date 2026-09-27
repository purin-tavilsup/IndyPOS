namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>A UTC instant and the store-local cash day it falls on, read together so they cannot straddle midnight.</summary>
public readonly record struct CashDrawerInstant(DateTime Utc, DateOnly BusinessDate);

public interface ICashDrawerClock
{
    CashDrawerInstant Now();
}
