namespace IndyPOS.Application.Interfaces;

public interface ICryptographyUtility
{
	string Encrypt(string input);
	string Decrypt(string input);
}