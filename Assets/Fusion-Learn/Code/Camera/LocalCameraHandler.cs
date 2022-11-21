using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCameraHandler : MonoBehaviour
{
    public Transform cameraAnchorPoint;

    Vector2 viewInput;              // Input
    float cameraRotationX = 0;      // Rotation
    float cameraRotationY = 0;      //

    // Other Components
    Camera localCamera;
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;


    private void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // We deactivate ALL other player cameras in NetworkPlayer.Spawned()
        // so if the camera is enabled, then this must be our local object
        // DETACH the camera from our Transform to prevent lag jitter
        if (localCamera.enabled)
            localCamera.transform.parent = null;
    }

    // Called AFTER the regular Update
    void LateUpdate()
    {
        if (cameraAnchorPoint == null)
            return;

        if (!localCamera.enabled)
            return;

        // Move camera to the player position
        localCamera.transform.position = cameraAnchorPoint.position;

        //=== APPLY ROTATION TO CAMERA ===
        // Get the view rotation from input
        cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);    // Clamp the view range - simulates realistic human view angles
        cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCustom.rotationSpeed;

        // Apply rotation
        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);  // Apply
    }

    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }
}
