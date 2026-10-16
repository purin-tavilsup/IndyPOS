namespace IndyPOS.Application.Common.Interfaces;

/// <summary>Who is logged in on this till right now; null between a logout and the next login.</summary>
public interface ICurrentUser
{
    ILoggedInUser? User { get; }
}
