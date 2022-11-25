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
        {
            // Only setting the Model (body eye and pupil) to invisible as we want to see gun
            if (trans.gameObject.name == "Model" ||
                trans.gameObject.name == "Body" ||
                trans.gameObject.name == "Eye" ||
                trans.gameObject.name == "Pupil") { trans.gameObject.layer = layerNumber; }
        }
    }
}
