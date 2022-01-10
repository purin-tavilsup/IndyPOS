namespace IndyPOS.Users
{
    public interface IUserAccountHelper
    {
		IUserAccount LoggedInUser { get; }

		bool IsLoggedIn { get; }
	}
}
