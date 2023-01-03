using IndyPOS.Application.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.Application.Adapters;

public class UserCredentialAdapter : IUserCredential
{
	private readonly UserCredential _adaptee;

	public UserCredentialAdapter(UserCredential adaptee)
	{
		_adaptee = adaptee;
	}

	public int UserId => _adaptee.UserId;

	public string Username => _adaptee.Username;

	public string Password => _adaptee.Password;
        
	public string DateCreated => _adaptee.DateCreated;

	public string DateUpdated => _adaptee.DateUpdated;
}