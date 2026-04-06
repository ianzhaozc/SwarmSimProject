using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class UavAgent : Agent
{
    private const int EnemyObservationSlots = 3;

    private UavPositionOptimizer optimizer;
    private UavParams selfParams;
    private EnvParams envParams;
    private BattleManager battleManager;
    private readonly List<UavParams> enemyBuffer = new List<UavParams>();
    private readonly List<DetectedEnemy> detectedEnemies = new List<DetectedEnemy>();

    private struct DetectedEnemy
    {
        public float Distance;
        public Vector3 Position;
    }

    public void InitializeRuntimeContext(EnvParams env, BattleManager manager)
    {
        envParams = env;
        battleManager = manager;
    }

    public override void Initialize()
    {
        optimizer = GetComponent<UavPositionOptimizer>();
        selfParams = GetComponent<UavParams>();
        if (envParams == null)
            envParams = FindObjectOfType<EnvParams>();
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();
    }

    public override void OnEpisodeBegin()
    {
        // 当前先不在这里做 reset
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;

        // 1) 自身状态（XZ + yaw[相对Z轴的弧度]）
        sensor.AddObservation(selfPos.x);
        sensor.AddObservation(selfPos.z);

        Vector3 forward = transform.forward;
        float yawZRad = Mathf.Atan2(forward.x, forward.z);
        sensor.AddObservation(yawZRad);

        // 2) 目标中心
        Vector3 target = (envParams != null) ? envParams.TargetCenter : Vector3.zero;
        sensor.AddObservation(target.x);
        sensor.AddObservation(target.z);

        // 3) 最近 K 个可探测敌人
        const int K = EnemyObservationSlots;

        if (selfParams == null || battleManager == null || envParams == null)
        {
            FillEmptyEnemyObservations(sensor, K);
            return;
        }

        List<DetectedEnemy> detectedEnemies = this.detectedEnemies;
        GetDetectedEnemies(selfPos, detectedEnemies);
        WriteNearestEnemyObservations(sensor, detectedEnemies, selfPos, envParams.LocalSenseRadius, K);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (optimizer == null) return;

        var ca = actions.ContinuousActions;
        if (ca.Length < 3) return;

        float targetX = ca[0];
        float targetZ = ca[1];
        float targetYaw = ca[2];   // 相对 Z 轴的夹角（弧度）

        Vector3 nextPos = new Vector3(targetX, transform.position.y, targetZ);
        optimizer.SetNextTarget(nextPos, targetYaw);
    }

    private void GetDetectedEnemies(Vector3 selfPos, List<DetectedEnemy> results)
    {
        results.Clear();
        battleManager.GetEnemyList(selfParams.Team, enemyBuffer);

        float localSenseR = envParams.LocalSenseRadius;
        bool enableGlobal = envParams.EnableGlobalDetection;
        float globalSenseR = envParams.GlobalDetectRadius;
        Vector3 center = envParams.TargetCenter;

        for (int i = 0; i < enemyBuffer.Count; i++)
        {
            UavParams enemy = enemyBuffer[i];
            if (enemy == null) continue;

            UavCombat combat = enemy.GetComponent<UavCombat>();
            if (combat != null && combat.IsDead) continue;

            Vector3 enemyPos = enemy.transform.position;
            float localDist = FlatDistance(selfPos, enemyPos);

            bool detectedByLocal = localDist <= localSenseR;
            bool detectedByGlobal = false;

            // 只有 defender 允许通过目标区共享探测发现敌人
            if (!detectedByLocal &&
                selfParams.Team == TeamType.Defender &&
                enableGlobal)
            {
                float distToCenter = FlatDistance(center, enemyPos);
                detectedByGlobal = distToCenter <= globalSenseR;
            }

            if (detectedByLocal || detectedByGlobal)
            {
                results.Add(new DetectedEnemy
                {
                    Distance = localDist,
                    Position = enemyPos
                });
            }
        }

        results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
    }

    private void WriteNearestEnemyObservations(
        VectorSensor sensor,
        List<DetectedEnemy> enemies,
        Vector3 selfPos,
        float localSenseR,
        int k)
    {
        for (int i = 0; i < k; i++)
        {
            if (i < enemies.Count)
            {
                Vector3 ep = enemies[i].Position;
                float dx = ep.x - selfPos.x;
                float dz = ep.z - selfPos.z;
                float distNorm = Mathf.Clamp01(enemies[i].Distance / localSenseR);

                sensor.AddObservation(dx);
                sensor.AddObservation(dz);
                sensor.AddObservation(distNorm);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    private void FillEmptyEnemyObservations(VectorSensor sensor, int k)
    {
        for (int i = 0; i < k; i++)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
