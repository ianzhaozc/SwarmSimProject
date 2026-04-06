using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UavPositionOptimizer : MonoBehaviour
{
    [SerializeField] private Vector3 nextBestPos;
    public Vector3 NextBestPos => nextBestPos;

    [SerializeField] private float nextBestYaw;
    public float NextBestYaw => nextBestYaw;

    [SerializeField] private bool controlByPython = true;
    public bool ControlByPython => controlByPython;

    private void Awake()
    {
        ResetTargetToCurrentPose();
    }

    public void SetNextTarget(Vector3 pos, float yaw)
    {
        nextBestPos = pos;
        nextBestYaw = yaw;
    }

    public void ResetTargetToCurrentPose()
    {
        nextBestPos = transform.position;

        Vector3 forward = transform.forward;
        nextBestYaw = Mathf.Atan2(forward.x, forward.z);
    }
}
