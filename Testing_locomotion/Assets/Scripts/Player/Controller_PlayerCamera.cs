using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine;

public class Controller_PlayerCamera : MonoBehaviour
{
    [Header("parameters")]
    public float sensitivity = 10.0f;
    public float cameraDistance = 30f;
    public float Camera_Smooth = 10f;
    public Transform target;

    Vector3 mouseDelta = Vector3.zero;
    Vector3 amount = Vector3.zero;

    public Vector3 addPos = new Vector3(0, 1.63f, 0);

    RaycastHit hit;
    float hitDistance = 0;
    float tanFOV;
    
    Camera cam;
    Vector3 LookAt = Vector3.zero;

    Vector3 cameraPosition = Vector3.zero;
    Vector3 cameraPositionNotOcc = Vector3.zero;
    Quaternion cameraRotation = quaternion.identity;

    Vector3 screenCenter = Vector3.zero;
    Vector3 up = Vector3.zero;
    Vector3 right = Vector3.zero;

    Vector3[] corners = new Vector3[5];

    void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        cam = gameObject.GetComponent<Camera>();
        float halfFOV = cam.fieldOfView * 0.5f * Mathf.Rad2Deg;
        tanFOV = Mathf.Tan(halfFOV) * cam.nearClipPlane;
    }

    
    void Update()
    {
        screenCenter = (cameraRotation * Vector3.forward) * cam.nearClipPlane;
        up = (cameraRotation * Vector3.up) * tanFOV;
        right = (cameraRotation * Vector3.right) * tanFOV* cam.aspect;

        corners[0] = cameraPosition + screenCenter - up - right;
        corners[1] = cameraPosition + screenCenter + up - right;
        corners[2] = cameraPosition + screenCenter + up + right;
        corners[3] = cameraPosition + screenCenter - up + right;
        corners[4] = cameraPosition + screenCenter;

        hitDistance = 1000000;

        for (int i = 0; i < 5; i++){
            if(Physics.Linecast(target.transform.position + addPos, corners[i], out hit)){
                Debug.DrawLine(target.transform.position + addPos, corners[i], Color.red);
                Debug.DrawRay(hit.point, Vector3.up * 0.05f, Color.white);
                hitDistance = Mathf.Min(hitDistance, hit.distance);
            } else {
                Debug.DrawLine(target.transform.position + addPos, corners[i], Color.blue);
            }
        } if(hitDistance > 999999) {
            hitDistance = 0;
        }
    }

    void LateUpdate()
    {
        mouseDelta.Set(Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y"),
            cameraDistance);
        
        amount += -mouseDelta * sensitivity;
        amount.z = Mathf.Clamp(amount.z, cameraDistance, 100);
        amount.y = Mathf.Clamp(amount.y, -75, 89);

        cameraRotation = Quaternion.AngleAxis(-amount.x, Vector3.up) *
                    Quaternion.AngleAxis(amount.y, Vector3.right);

        LookAt = cameraRotation * Vector3.forward;

        cameraPosition = target.transform.position + addPos - LookAt * amount.z * 0.1f;

        cameraPositionNotOcc = target.transform.position + addPos - LookAt * hitDistance;

        transform.rotation = Quaternion.Lerp(transform.rotation, cameraRotation, Time.deltaTime * Camera_Smooth);

        if(hitDistance > 0){
            transform.position = Vector3.Lerp(transform.position, cameraPositionNotOcc, Time.deltaTime * Camera_Smooth);
        } else {
            transform.position = Vector3.Lerp(transform.position, cameraPosition, Time.deltaTime * Camera_Smooth);
        }
    }
}
