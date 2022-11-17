using IndyPOS.Facade.Interfaces;
using UserCredentialModel = IndyPOS.DataAccess.Models.UserCredential;

namespace IndyPOS.Facade.Adapters;

public class UserCredentialAdapter : IUserCredential
{
	private readonly UserCredentialModel _adaptee;

	public UserCredentialAdapter(UserCredentialModel adaptee)
	{
		_adaptee = adaptee;
	}

	public int UserId => _adaptee.UserId;

	public string Username => _adaptee.Username;

	public string Password => _adaptee.Password;
        
	public string DateCreated => _adaptee.DateCreated;

	public string DateUpdated => _adaptee.DateUpdated;
}