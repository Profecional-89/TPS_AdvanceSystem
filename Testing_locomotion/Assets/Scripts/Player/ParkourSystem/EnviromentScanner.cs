using System.Data.Common;
using UnityEngine;

public class EnviromentScanner : MonoBehaviour
{
    [SerializeField] Vector3 forwardRayOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] LayerMask ObstacleLayer;
    [SerializeField] float forwardRayLenght = 0.8f;
    [SerializeField] float heightRayLenght = 5f;

    public ObstacleData ObstacleCheck()
    {
        var hitData = new ObstacleData();

        var forwardOrigin = transform.position + forwardRayOffset;
        hitData.forwardHitFound = Physics.Raycast(forwardOrigin, transform.forward,
            out hitData.forwardHit, forwardRayLenght, ObstacleLayer);

        Debug.DrawRay(forwardOrigin, transform.forward * forwardRayLenght, (hitData.forwardHitFound) ? Color.red : Color.white);

        if (hitData.forwardHitFound)
        {
            var HeightOrigin = hitData.forwardHit.point + Vector3.up * heightRayLenght;
            hitData.HeightHitFound = Physics.Raycast(HeightOrigin, Vector3.down,
                out hitData.heightHit, heightRayLenght, ObstacleLayer);

            Debug.DrawRay(HeightOrigin, Vector3.down * heightRayLenght, (hitData.HeightHitFound) ? Color.red : Color.white);
        }

        return hitData;
    }
}

public struct ObstacleData
{
    public bool forwardHitFound;
    public bool HeightHitFound;
    public RaycastHit forwardHit;
    public RaycastHit heightHit;
}
