namespace IndyPOS.Users
{
    internal interface IUserAccountHelper
    {
		IUserAccount LoggedInUser { get; }

		bool IsLoggedIn { get; }
	}
}
