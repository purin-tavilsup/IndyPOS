namespace IndyPOS.Infrastructure.Services.RawDeviceInput
{
	public class PreMessageFilter : IMessageFilter
	{
		// true  to filter the message and stop it from being dispatched 
		// false to allow the message to continue to the next filter or control.
		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg != Win32.WM_INPUT)
			{
				// Allow any non WM_INPUT message to pass through
				return false;
			}

			return m.Msg == Win32.WM_KEYDOWN;
		}
	}
}