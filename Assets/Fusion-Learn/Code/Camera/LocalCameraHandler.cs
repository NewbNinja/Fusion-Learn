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
    public Camera localCamera;
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;


    private void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
    }

    // Start is called before the first frame update
    void Start()
    {
        cameraRotationX = GameManager.instance.cameraViewRotation.x;
        cameraRotationY = GameManager.instance.cameraViewRotation.y;
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

    private void OnDestroy()
    {
        // Make sure we're the LOCAL camera (our clients cam) and save the current view before we destroy
        // so we can use it again when we reconnect after a disconnection / host migration
        
        if (cameraRotationX != 0 && cameraRotationY != 0)
        {
            GameManager.instance.cameraViewRotation.x = cameraRotationX;
            GameManager.instance.cameraViewRotation.y = cameraRotationY;
        }
    }
}
