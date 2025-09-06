using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Parkour System/New Parkour Action")]
public class ParkourAction : ScriptableObject
{
    [SerializeField] string triggerName;

    [SerializeField] float minHeight;
    [SerializeField] float maxHeight;

    [SerializeField] bool rotateToObstacle = true;

    [Header("Target Matching")]
    [SerializeField] bool enableTargetMatching = true;
    [SerializeField] AvatarTarget matchBodyPart;
    [SerializeField] float matchStartTime;
    [SerializeField] float matchTargetTime;

    public quaternion TargetRotation { get; set; }
    public Vector3 MatchPos { get; set; }

    public bool CheckIfPossible(ObstacleData hitData, Transform player)
    {
        float height = hitData.heightHit.point.y - player.position.y;
        if (height < minHeight || height > maxHeight)
            return false;
        if (rotateToObstacle)
            TargetRotation = Quaternion.LookRotation(-hitData.forwardHit.normal);

        if (EnableTargetMatching)
            MatchPos = hitData.heightHit.point;

        return true;
    }
    public string AnimName => triggerName;
    public bool RotateToObstacle => rotateToObstacle;

    public bool EnableTargetMatching => enableTargetMatching;
    public AvatarTarget MatchBodyPart => matchBodyPart;
    public float MatchStartTime => matchStartTime;
    public float MatchTargetTime => matchTargetTime;
}
 