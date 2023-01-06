﻿using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class UserCredential : IUserCredential
{
    public int UserId { get; }

    public string Username { get; }

    public string Password { get; }

    public string DateCreated { get; }

    public string DateUpdated { get; }
}