﻿namespace IndyPOS.Facade.Interfaces
{
	public interface ICryptographyHelper
	{
		string Encrypt(string input);
		string Decrypt(string input);
	}
}