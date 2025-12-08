using Mirror;
using UnityEngine;

namespace Catnip.Scripts._Systems.Slots {
public class IdentityInternal : NetworkBehaviour {
    [SyncVar(hook = nameof(OnInternalObjectChange))]
    public GameObject internalObject;

    public void OnInternalObjectChange(GameObject oldValue, GameObject newValue) { }
}
}