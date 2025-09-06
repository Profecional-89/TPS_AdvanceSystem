using System.Collections.Generic;
using UnityEngine;
using System.Collections;


public class ParkourController : MonoBehaviour
{
    [SerializeField] List<ParkourAction> parkourActions;

    public bool inAction;

    EnviromentScanner enviromentScanner;
    Animator animator;
    PlayerLocomotion Player;
    private void Awake()
    {
        enviromentScanner = GetComponent<EnviromentScanner>();
        animator = GetComponent<Animator>();
        Player = GetComponent<PlayerLocomotion>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump") && !inAction)
        {
            var hitData = enviromentScanner.ObstacleCheck();
            if (hitData.forwardHitFound)
            {
                foreach (var action in parkourActions)
                {
                    if (action.CheckIfPossible(hitData, transform))
                    {
                        StartCoroutine(DoParkourAction(action));
                        break;
                    }
                }
            }
        }

    }

    IEnumerator DoParkourAction(ParkourAction action)
    {
        inAction = true;
        //animator.SetTrigger(action.AnimName);
        animator.CrossFade(action.AnimName, 0.045f);
        Player.HasControl = false;
        yield return null;

        var animState = animator.GetNextAnimatorStateInfo(0);

        float timer = 0f;
        while (timer <= animState.length)
        {
            timer += Time.deltaTime;

            // roatet player
            if (action.RotateToObstacle)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, action.TargetRotation, 500f * Time.deltaTime);

            if (action.EnableTargetMatching)
                MatchTarget(action);

                yield return null;
        }

        Player.HasControl = true;
        inAction = false;

    }

    void MatchTarget(ParkourAction action)
    {
        if (animator.isMatchingTarget) return;

        animator.MatchTarget(action.MatchPos, transform.rotation, action.MatchBodyPart, new MatchTargetWeightMask(new Vector3(0, 1, 1), 0),
            action.MatchStartTime, action.MatchTargetTime);
    }
}
