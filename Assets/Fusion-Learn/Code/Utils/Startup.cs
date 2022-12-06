using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]  // Anything after this statement is executed BEFORE anything is loaded in Unity (Great for GAME MANAGERS etc)
    public static void InstantiatePrefabs()
    {
        Debug.Log("-- Instantiating objects --");

        // Instantiate all of the game objects stored in our project Resources/InstatiateOnLoad/ directory
        GameObject[] prefabsToInstantiate = Resources.LoadAll<GameObject>("InstantiateOnLoad/");

        foreach (GameObject prefab in prefabsToInstantiate)
        {
            Debug.Log($"Creating {prefab.name}");
            GameObject.Instantiate(prefab);
        }

        Debug.Log("-- Instantiating objects done --");
    }
}
