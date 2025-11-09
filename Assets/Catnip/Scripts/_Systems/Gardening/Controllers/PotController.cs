using System.Collections;
using Mirror;
using UnityEngine;
namespace Catnip.Scripts._Systems.Gardening {
public class PotController : NetworkBehaviour {
    [SerializeField] GameObject groundObject;
    [SerializeField] GameObject growStartObject;
    [SerializeField] GameObject growFinishObject;

    public GrowState growState = GrowState.Empty;
    int progress = 0;
    int totalSteps = 2;
    // Seed seed;

    public void AddSoil() {
        Debug.Log("AddSoil()");
        growState = GrowState.Soiled;
        RpcAddSoil();
        groundObject.SetActive(true);
    }

    [ClientRpc]
    public void RpcAddSoil() {
        groundObject.SetActive(true);
    }
    
    public void AddWater() {
        Debug.Log("AddWater()");
        growState = GrowState.Watered;
    }

    public void AddSeeds() {
        Debug.Log("AddSeeds()");
        growState = GrowState.Planted;
        StartGrowing();
    }

    private void StartGrowing() {
        progress = 1;
        growStartObject.SetActive(true);
        RpcStartGrowing();
        StartCoroutine(PourRoutine());
    }
    
    [ClientRpc]
    public void RpcStartGrowing() {
        growStartObject.SetActive(true);
    }
    
    private IEnumerator PourRoutine() {
        yield return new WaitForSeconds(10f);
        progress = 2;
        growStartObject.SetActive(false);
        growFinishObject.SetActive(true);
        RpcFinishGrowing();
        growState = GrowState.Grown;
    }
    
    [ClientRpc]
    public void RpcFinishGrowing() {
        growStartObject.SetActive(false);
        growFinishObject.SetActive(true);
    }
}
}
