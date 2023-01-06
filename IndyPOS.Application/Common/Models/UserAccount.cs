using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class UserAccount : IUserAccount
{
    public int UserId { get; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public int RoleId { get; set; }

    public string DateCreated { get; }

    public string DateUpdated { get; }
}