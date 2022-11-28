//  USAGE:
//
//  ATTACH TO ANY GAMEOBJECT YOU WANT TO DESTROY AFTER x AMOUNT OF SECONDS

using System.Collections;
using UnityEngine;

public class DestroyGameObject : MonoBehaviour
{
    public float lifeTime = 1.5f;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }
}
