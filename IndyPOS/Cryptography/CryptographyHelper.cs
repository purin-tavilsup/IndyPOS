using IndyPOS.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace IndyPOS.Cryptography
{
	public class CryptographyHelper : ICryptographyHelper
	{
		private const string Hash = "b415407e-3f08-43be-8130-46f8146467f7";

		public string Encrypt(string input)
		{
			var data = Encoding.UTF8.GetBytes(input);

			using (var md5 = new MD5CryptoServiceProvider())
			{
				var keys = md5.ComputeHash(Encoding.UTF8.GetBytes(Hash));

				using (var tripleDes = new TripleDESCryptoServiceProvider {Key = keys, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7})
				{
					var transform = tripleDes.CreateEncryptor();
					var results = transform.TransformFinalBlock(data, 0, data.Length);

					return Convert.ToBase64String(results, 0, results.Length);
				}
			}
		}

		public string Decrypt(string input)
		{
			var data = Convert.FromBase64String(input);

			using (var md5 = new MD5CryptoServiceProvider())
			{
				var keys = md5.ComputeHash(Encoding.UTF8.GetBytes(Hash));

				using (var tripDes = new TripleDESCryptoServiceProvider { Key = keys, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7 })
				{
					var transform = tripDes.CreateDecryptor();
					var results = transform.TransformFinalBlock(data, 0, data.Length);

					return Encoding.UTF8.GetString(results);
				}
			}
		}
	}
}
