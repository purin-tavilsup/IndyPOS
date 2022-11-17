using IndyPOS.Facade.Interfaces;
using UserAccountModel = IndyPOS.DataAccess.Models.UserAccount;

namespace IndyPOS.Facade.Adapters;

public class UserAccountAdapter : IUserAccount
{
	private readonly UserAccountModel _adaptee;

	public UserAccountAdapter(UserAccountModel adaptee)
	{
		_adaptee = adaptee;
	}

	public int UserId => _adaptee.UserId;

	public string FirstName 
	{ 
		get => _adaptee.FirstName; 
		set => _adaptee.FirstName = value;
	}

	public string LastName 
	{ 
		get => _adaptee.LastName; 
		set => _adaptee.LastName = value;
	}

	public int RoleId
	{
		get => _adaptee.RoleId;
		set => _adaptee.RoleId = value;
	}

	public string DateCreated => _adaptee.DateCreated;

	public string DateUpdated => _adaptee.DateUpdated;
}