using UnityEngine;

public static class Utils
{
    public static Vector3 GetRandomSpawnPoint()
    {
        return new Vector3(Random.Range(-20, 20), 15, Random.Range(-20, 20));
    }

    // Get ALL transforms in a model (including inactive objects) -- then set all their layer numbers
    public static void SetRenderLayerInChildren(Transform transform, int layerNumber)
    {
        // NOTES:  We've set the camera on the NetworkPlayerPF (prefab) to ignore layer:6 ("LocalPlayerModel")
        // We do this so that the camera ignores the local players prefab so that is doesn't block our view
        // We use this in NetworkPlayer.cs
        foreach (Transform trans in transform.GetComponentsInChildren<Transform>(true))
            trans.gameObject.layer = layerNumber;
    }
}
