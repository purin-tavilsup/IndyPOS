using IndyPOS.Windows.Forms.Enums;
using Prism.Events;

namespace IndyPOS.Windows.Forms.Events;

/// <summary>
/// Event for notifying the active sub-panel has changed on the Main form.
/// </summary>
public class ActiveSubPanelChangedEvent : PubSubEvent<SubPanel>
{
}