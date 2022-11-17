using IndyPOS.Common.Enums;
using Prism.Events;

namespace IndyPOS.Facade.Events;

/// <summary>
/// Event for notifying the active sub-panel has changed on the Main form.
/// </summary>
public class ActiveSubPanelChangedEvent : PubSubEvent<SubPanel>
{
}