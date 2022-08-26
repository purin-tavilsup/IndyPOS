using IndyPOS.Interfaces;

namespace IndyPOS.Users
{
    internal class UserAccountHelper : IUserAccountHelper
    {
		private readonly IUserController _userController;

		public IUserAccount LoggedInUser => _userController.LoggedInUser;

		public bool IsLoggedIn => _userController.IsLoggedIn;

        public UserAccountHelper(IUserController userController)
        {
			_userController = userController;
        }
    }
}
