using System.Collections.Generic;
using UnityEngine;

public class DubinsController : MonoBehaviour
{
    private EnvParams envParams;
    private UavPositionOptimizer uavPositionOptimizer;

    private float speed;
    private float turningRadius;
    private float stepSize;

    private Vector3 startPos;
    private float startYaw;
    private Vector3 endPos;
    private float endYaw;

    private List<Vector3> pathPoints;
    private int currentIndex = 0;

    private Vector3 lastEndPos;
    private float lastEndYaw;

    [Header("Replan Thresholds")]
    public float replanPosThreshold = 0.01f;
    public float replanYawThreshold = 0.05f;

    [Header("Move Tuning")]
    public float arriveDist = 0.05f;
    public float rotateLerpSpeed = 12f;

    public void InitializeRuntimeContext(EnvParams env)
    {
        envParams = env;
        RefreshMovementParams();
    }

    private void Start()
    {
        uavPositionOptimizer = GetComponent<UavPositionOptimizer>();

        if (envParams == null)
            envParams = FindObjectOfType<EnvParams>();

        if (envParams == null)
        {
            Debug.LogError("DubinsController: EnvParams not found.");
            enabled = false;
            return;
        }

        RefreshMovementParams();

        lastEndPos = new Vector3(float.NaN, float.NaN, float.NaN);
        lastEndYaw = float.NaN;
    }

    private void Update()
    {
        stepSize = speed * Time.deltaTime;

        UpdateTargetAndPath();
        FollowPath();
    }

    private void UpdateTargetAndPath()
    {
        if (uavPositionOptimizer == null) return;

        endPos = uavPositionOptimizer.NextBestPos;
        endYaw = Mathf.PI * 0.5f - uavPositionOptimizer.NextBestYaw;

        bool needReplan = pathPoints == null || pathPoints.Count == 0;

        if (!needReplan)
        {
            float posDelta = Vector3.Distance(endPos, lastEndPos);
            float yawDelta = Mathf.Abs(endYaw - lastEndYaw);

            if (posDelta > replanPosThreshold) needReplan = true;
            if (yawDelta > replanYawThreshold) needReplan = true;
        }

        if (needReplan)
        {
            startPos = transform.position;
            startYaw = (90f - transform.eulerAngles.y) * Mathf.Deg2Rad;

            pathPoints = DubinsCalculator.GeneratePathPoints(
                startPos,
                startYaw,
                endPos,
                endYaw,
                turningRadius,
                stepSize
            );

            currentIndex = (pathPoints != null && pathPoints.Count >= 2) ? 1 : 0;
            lastEndPos = endPos;
            lastEndYaw = endYaw;
        }
    }

    private void FollowPath()
    {
        if (pathPoints == null || pathPoints.Count == 0) return;
        if (currentIndex >= pathPoints.Count) return;

        Vector3 targetPoint = pathPoints[currentIndex];
        targetPoint.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint,
            speed * Time.deltaTime
        );

        Vector3 dir = targetPoint - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotateLerpSpeed * Time.deltaTime
            );
        }

        if (Vector3.Distance(transform.position, targetPoint) < arriveDist)
        {
            currentIndex++;
        }
    }

    private void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Count < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
        }
    }

    private void RefreshMovementParams()
    {
        if (envParams == null) return;

        speed = envParams.Speed;
        turningRadius = envParams.MinTurnRadius;
    }
}
