using UnityEngine;
namespace Catnip.Scripts._Systems.Gardening {
public interface IUsable {
    public void ClientUse(Ray ray);
    public void ServerUse(Ray ray);
}
}
