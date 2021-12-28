using Prism.Events;
using IndyPOS.Enums;

namespace IndyPOS.Events
{
	/// <summary>
	/// Event for notifying the active sub-panel has changed on the Main form.
	/// </summary>
	public class ActiveSubPanelChangedEvent : PubSubEvent<Subpanel>
	{
	}
}
