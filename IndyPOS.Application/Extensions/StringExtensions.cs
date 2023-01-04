﻿namespace IndyPOS.Application.Extensions;

public static class StringExtensions
{
	public static bool HasValue(this string value)
	{
		return !string.IsNullOrWhiteSpace(value);
	}
}