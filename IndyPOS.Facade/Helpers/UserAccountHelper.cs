using IndyPOS.Facade.Interfaces;

namespace IndyPOS.Facade.Helpers
{
    public class UserAccountHelper : IUserAccountHelper
    {
		private readonly IUserHelper _userController;

		public IUserAccount LoggedInUser => _userController.LoggedInUser;

		public bool IsLoggedIn => _userController.IsLoggedIn;

        public UserAccountHelper(IUserHelper userController)
        {
			_userController = userController;
        }
    }
}
