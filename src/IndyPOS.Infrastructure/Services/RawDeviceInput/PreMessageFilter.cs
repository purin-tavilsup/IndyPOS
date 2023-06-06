namespace IndyPOS.Infrastructure.Services.RawDeviceInput
{
	public class PreMessageFilter : IMessageFilter
	{
		// true  to filter the message and stop it from being dispatched 
		// false to allow the message to continue to the next filter or control.
		public bool PreFilterMessage(ref Message m)
		{
			var keyCode = (Keys)(int)m.WParam & Keys.KeyCode;

			if (m.Msg == Win32.WM_KEYDOWN && keyCode == Keys.Enter)
			{
				// Ignore Enter key
				return true;
			}

			return false;
		}
	}
}