using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Sales {
public interface ISellable {
    public void Sell(Ray ray, NetworkConnectionToClient sender);
}
}
