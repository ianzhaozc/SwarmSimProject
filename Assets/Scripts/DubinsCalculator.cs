using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DubinsSegmentType { L, R, S }

public struct DubinsPath
{
    public DubinsSegmentType[] pathType;
    public float[] segmentLengths;
    public float totalLength;
    public bool isValid;
}

public static class DubinsCalculator
{
    public const float TwoPI = Mathf.PI * 2f;
    public const float Epsilon = 1e-4f;
    public static List<Vector3> GeneratePathPoints(Vector3 startPos, float startYaw, Vector3 endPos, float endYaw, float turningRadius, float stepSize)
    {
        DubinsPath path = CalculateDubinsPath(startPos, startYaw, endPos, endYaw, turningRadius);
        if (!path.isValid)
        {
            Debug.LogWarning("无法生成有效的Dubins路径");
            return null;
        }
        List<Vector3> points = new List<Vector3>();
        Vector3 currentPos = startPos;
        float currentYaw = startYaw;
        points.Add(currentPos);
        for (int i = 0; i < 3; i++)
        {
            float segmentLength = path.segmentLengths[i];
            DubinsSegmentType segmentType = path.pathType[i];
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(segmentLength / stepSize)));
            float stepLength = segmentLength / steps;
            for (int j = 0; j < steps; j++)
            {
                switch (segmentType)
                {
                    case DubinsSegmentType.S:
                        currentPos += new Vector3(Mathf.Cos(currentYaw), 0, Mathf.Sin(currentYaw)) * stepLength;
                        break;
                    case DubinsSegmentType.L:
                        currentPos = MoveAlongArc(currentPos, currentYaw, turningRadius, stepLength, 1);
                        currentYaw += stepLength / turningRadius;
                        break;
                    case DubinsSegmentType.R:
                        currentPos = MoveAlongArc(currentPos, currentYaw, turningRadius, stepLength, -1);
                        currentYaw -= stepLength / turningRadius;
                        break;
                }
                currentYaw = NormalizeAngle(currentYaw);
                points.Add(currentPos);
            }
        }
        return points;
    }
    public static DubinsPath CalculateDubinsPath(Vector3 startPos, float startYaw, Vector3 endPos, float endYaw, float turningRadius)
    {
        Vector2 p0 = new Vector2(startPos.x, startPos.z);
        Vector2 p1 = new Vector2(endPos.x, endPos.z);
        DubinsPath[] possiblePaths = new DubinsPath[6];
        possiblePaths[0] = CalculateLSL(p0, startYaw, p1, endYaw, turningRadius);
        possiblePaths[1] = CalculateRSR(p0, startYaw, p1, endYaw, turningRadius);
        possiblePaths[2] = CalculateLSR(p0, startYaw, p1, endYaw, turningRadius);
        possiblePaths[3] = CalculateRSL(p0, startYaw, p1, endYaw, turningRadius);
        possiblePaths[4] = CalculateLRL(p0, startYaw, p1, endYaw, turningRadius);
        possiblePaths[5] = CalculateRLR(p0, startYaw, p1, endYaw, turningRadius);
        DubinsPath shortestPath = new DubinsPath();
        shortestPath.totalLength = float.MaxValue;
        foreach (var path in possiblePaths)
        {
            if (path.isValid && path.totalLength < shortestPath.totalLength)
            {
                shortestPath = path;
            }
        }
        return shortestPath;
    }
    private static DubinsPath CalculateLSL(Vector2 p0, float yaw0, Vector2 p1, float yaw1, float r)
    {
        DubinsPath path = new DubinsPath();
        path.pathType = new DubinsSegmentType[] { DubinsSegmentType.L, DubinsSegmentType.S, DubinsSegmentType.L };
        path.segmentLengths = new float[3];
        float dz = p1.y - p0.y;
        float dx = p1.x - p0.x;
        float delta = Mathf.Atan2(dz, dx);
        float dist = Mathf.Sqrt(dz * dz + dx * dx);
        float d = dist / r;
        float alpha = NormalizeAngle(yaw0 - delta);
        float beta = NormalizeAngle(yaw1 - delta);
        float t = NormalizeAngle(-alpha + Mathf.Atan2(Mathf.Cos(beta) - Mathf.Cos(alpha), d + Mathf.Sin(alpha) - Mathf.Sin(beta)));
        float p2 = 2f + d * d - 2f * Mathf.Cos(alpha - beta) + 2f * d * (Mathf.Sin(alpha) - Mathf.Sin(beta));
        if (p2 < 0) { path.isValid = false; return path; }
        float p = Mathf.Sqrt(p2);
        float q = NormalizeAngle(beta - Mathf.Atan2(Mathf.Cos(beta) - Mathf.Cos(alpha), d + Mathf.Sin(alpha) - Mathf.Sin(beta)));
        path.segmentLengths[0] = r * t;
        path.segmentLengths[1] = r * p;
        path.segmentLengths[2] = r * q;
        path.totalLength = path.segmentLengths[0] + path.segmentLengths[1] + path.segmentLengths[2];
        path.isValid = true; return path;
    }
    private static DubinsPath CalculateRSR(Vector2 p0, float yaw0, Vector2 p1, float yaw1, float r)
    {
        DubinsPath path = new DubinsPath();
        path.pathType = new DubinsSegmentType[] { DubinsSegmentType.R, DubinsSegmentType.S, DubinsSegmentType.R };
        path.segmentLengths = new float[3];
        float dz = p1.y - p0.y;
        float dx = p1.x - p0.x;
        float delta = Mathf.Atan2(dz, dx);
        float dist = Mathf.Sqrt(dz * dz + dx * dx);
        float d = dist / r;
        float alpha = NormalizeAngle(yaw0 - delta);
        float beta = NormalizeAngle(yaw1 - delta);
        float t = NormalizeAngle(alpha - Mathf.Atan2(Mathf.Cos(alpha) - Mathf.Cos(beta), d - Mathf.Sin(alpha) + Mathf.Sin(beta)));
        float p2 = 2f + d * d - 2f * Mathf.Cos(alpha - beta) + 2f * d * (Mathf.Sin(beta) - Mathf.Sin(alpha));
        if (p2 < 0) { path.isValid = false; return path; }
        float p = Mathf.Sqrt(p2);
        float q = NormalizeAngle(-beta + Mathf.Atan2(Mathf.Cos(alpha) - Mathf.Cos(beta), d - Mathf.Sin(alpha) + Mathf.Sin(beta)));
        path.segmentLengths[0] = r * t;
        path.segmentLengths[1] = r * p;
        path.segmentLengths[2] = r * q;
        path.totalLength = path.segmentLengths[0] + path.segmentLengths[1] + path.segmentLengths[2];
        path.isValid = true; return path;
    }
    private static DubinsPath CalculateLSR(Vector2 p0, float yaw0, Vector2 p1, float yaw1, float r)
    {
        DubinsPath path = new DubinsPath();
        path.pathType = new DubinsSegmentType[] { DubinsSegmentType.L, DubinsSegmentType.S, DubinsSegmentType.R };
        path.segmentLengths = new float[3];
        float dz = p1.y - p0.y;
        float dx = p1.x - p0.x;
        float delta = Mathf.Atan2(dz, dx);
        float dist = Mathf.Sqrt(dz * dz + dx * dx);
        float d = dist / r;
        float alpha = NormalizeAngle(yaw0 - delta);
        float beta = NormalizeAngle(yaw1 - delta);
        float p2 = -2f + d * d + 2f * Mathf.Cos(alpha - beta) + 2f * d * (Mathf.Sin(alpha) + Mathf.Sin(beta));
        if (p2 < 0) { path.isValid = false; return path; }
        float p = Mathf.Sqrt(p2);
        float t = NormalizeAngle(-alpha + Mathf.Atan2(-Mathf.Cos(alpha) - Mathf.Cos(beta), d + Mathf.Sin(alpha) + Mathf.Sin(beta)) - Mathf.Atan2(-2f, p));
        float q = NormalizeAngle(-beta + Mathf.Atan2(-Mathf.Cos(alpha) - Mathf.Cos(beta), d + Mathf.Sin(alpha) + Mathf.Sin(beta)) - Mathf.Atan2(-2f, p));
        path.segmentLengths[0] = r * t;
        path.segmentLengths[1] = r * p;
        path.segmentLengths[2] = r * q;
        path.totalLength = path.segmentLengths[0] + path.segmentLengths[1] + path.segmentLengths[2];
        path.isValid = true; return path;
    }
    private static DubinsPath CalculateRSL(Vector2 p0, float yaw0, Vector2 p1, float yaw1, float r)
    {
        DubinsPath path = new DubinsPath();
        path.pathType = new DubinsSegmentType[] { DubinsSegmentType.R, DubinsSegmentType.S, DubinsSegmentType.L };
        path.segmentLengths = new float[3];
        float dz = p1.y - p0.y;
        float dx = p1.x - p0.x;
        float delta = Mathf.Atan2(dz, dx);
        float dist = Mathf.Sqrt(dz * dz + dx * dx);
        float d = dist / r;
        float alpha = NormalizeAngle(yaw0 - delta);
        float beta = NormalizeAngle(yaw1 - delta);
        float p2 = d * d - 2f + 2f * Mathf.Cos(alpha - beta) - 2f * d * (Mathf.Sin(alpha) + Mathf.Sin(beta));
        if (p2 < 0) { path.isValid = false; return path; }
        float p = Mathf.Sqrt(p2);
        float t = NormalizeAngle(alpha - Mathf.Atan2(Mathf.Cos(alpha) + Mathf.Cos(beta), d - Mathf.Sin(alpha) - Mathf.Sin(beta)) + Mathf.Atan2(2f, p));
        float q = NormalizeAngle(beta - Mathf.Atan2(Mathf.Cos(alpha) + Mathf.Cos(beta), d - Mathf.Sin(alpha) - Mathf.Sin(beta)) + Mathf.Atan2(2f, p));
        path.segmentLengths[0] = r * t;
        path.segmentLengths[1] = r * p;
        path.segmentLengths[2] = r * q;
        path.totalLength = path.segmentLengths[0] + path.segmentLengths[1] + path.segmentLengths[2];
        path.isValid = true; return path;

    }
    private static DubinsPath CalculateLRL(Vector2 p0, float yaw0, Vector2 p1, float yaw1, float r)
    {
        DubinsPath path = new DubinsPath();
        path.pathType = new DubinsSegmentType[] { DubinsSegmentType.L, DubinsSegmentType.R, DubinsSegmentType.L };
        path.segmentLengths = new float[3];
        float dz = p1.y - p0.y;
        float dx = p1.x - p0.x;
        float delta = Mathf.Atan2(dz, dx);
        float dist = Mathf.Sqrt(dz * dz + dx * dx);
        if (dist >= 4f * r) { path.isValid = false; return path; }
        float d = dist / r;
        float alpha = NormalizeAngle(yaw0 - delta);
        float beta = NormalizeAngle(yaw1 - delta);
        float acosArg = (6f - d * d + 2f * Mathf.Cos(alpha - beta) + 2f * d * (-Mathf.Sin(alpha) + Mathf.Sin(beta))) / 8f;
        if (acosArg < -1f || acosArg > 1f) { path.isValid = false; return path; }
        float p = NormalizeAngle(TwoPI - Mathf.Acos(acosArg));
        float t = NormalizeAngle(-alpha - Mathf.Atan2(Mathf.Cos(alpha) - Mathf.Cos(beta), d + Mathf.Sin(alpha) - Mathf.Sin(beta)) + p * 0.5f);
        float q = NormalizeAngle(beta - alpha - t + p);
        path.segmentLengths[0] = r * t;
        path.segmentLengths[1] = r * p;
        path.segmentLengths[2] = r * q;
        path.totalLength = path.segmentLengths[0] + path.segmentLengths[1] + path.segmentLengths[2];
        path.isValid = true; return path;
    }
    private static DubinsPath CalculateRLR(Vector2 p0, float yaw0, Vector2 p1, float yaw1, float r)
    {
        DubinsPath path = new DubinsPath();
        path.pathType = new DubinsSegmentType[] { DubinsSegmentType.R, DubinsSegmentType.L, DubinsSegmentType.R };
        path.segmentLengths = new float[3];
        float dz = p1.y - p0.y;
        float dx = p1.x - p0.x;
        float delta = Mathf.Atan2(dz, dx);
        float dist = Mathf.Sqrt(dz * dz + dx * dx);
        if (dist >= 4f * r) { path.isValid = false; return path; }
        float d = dist / r;
        float alpha = NormalizeAngle(yaw0 - delta);
        float beta = NormalizeAngle(yaw1 - delta);
        float acosArg = (6f - d * d + 2f * Mathf.Cos(alpha - beta) + 2f * d * (Mathf.Sin(alpha) - Mathf.Sin(beta))) / 8f;
        if (acosArg < -1f || acosArg > 1f) { path.isValid = false; return path; }
        float p = NormalizeAngle(TwoPI - Mathf.Acos(acosArg));
        float t = NormalizeAngle(alpha - Mathf.Atan2(Mathf.Cos(alpha) - Mathf.Cos(beta), d - Mathf.Sin(alpha) + Mathf.Sin(beta)) + p * 0.5f);
        float q = NormalizeAngle(alpha - beta - t + p);
        path.segmentLengths[0] = r * t;
        path.segmentLengths[1] = r * p;
        path.segmentLengths[2] = r * q;
        path.totalLength = path.segmentLengths[0] + path.segmentLengths[1] + path.segmentLengths[2];
        path.isValid = true; return path;
    }
    public static float NormalizeAngle(float angle)
    {
        float res = angle % TwoPI;
        if (res < 0) res += TwoPI;
        return res;
    }
    private static Vector3 MoveAlongArc(Vector3 currentPos, float currentYaw, float radius, float arcLength, int turnDirection)
    {
        float deltaAngle = arcLength / radius;
        Vector3 circleCenter = currentPos + new Vector3(-Mathf.Sin(currentYaw), 0, Mathf.Cos(currentYaw)) * radius * turnDirection;
        Vector3 toPoint = currentPos - circleCenter;
        Quaternion rotation = Quaternion.Euler(0, -turnDirection * deltaAngle * Mathf.Rad2Deg, 0);
        Vector3 rotatedVector = rotation * toPoint;
        return circleCenter + rotatedVector;
    }

}





