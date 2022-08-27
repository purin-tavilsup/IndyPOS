namespace IndyPOS.Facade.Interfaces
{
    public interface IUserAccountHelper
    {
		IUserAccount LoggedInUser { get; }

		bool IsLoggedIn { get; }
	}
}
