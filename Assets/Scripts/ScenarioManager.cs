using System.Collections.Generic;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnvParams envParams;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private GameObject attackerPrefab;
    [SerializeField] private GameObject defenderPrefab;

    [Header("Visuals")]
    [SerializeField] private Material attackerMaterial;
    [SerializeField] private Material defenderMaterial;

    [Header("Spawn")]
    [SerializeField] private float spawnY = 0f;

    private readonly List<GameObject> spawnedUnits = new List<GameObject>();

    private void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        ClearAll();

        if (battleManager != null)
            battleManager.ClearAll();

        if (envParams == null)
        {
            Debug.LogError("ScenarioManager: EnvParams is null.");
            return;
        }

        SpawnDefenders();
        SpawnAttackers();
    }

    public void ClearAll()
    {
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            if (spawnedUnits[i] != null)
                Destroy(spawnedUnits[i]);
        }
        spawnedUnits.Clear();
    }

    private void SpawnDefenders()
    {
        int n = envParams.DefenderCount;
        float r = envParams.DefenderPatrolRadius;
        Vector3 c = envParams.TargetCenter;

        for (int i = 0; i < n; i++)
        {
            float angle = i * Mathf.PI * 2f / n;

            float x = c.x + r * Mathf.Cos(angle);
            float z = c.z + r * Mathf.Sin(angle);

            Vector3 pos = new Vector3(x, spawnY, z);

            // 逆时针巡逻的切线方向
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            float yaw = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;

            GameObject go = Instantiate(defenderPrefab, pos, Quaternion.Euler(0f, yaw, 0f));
            go.name = $"Defender_{i}";
            ApplyMaterial(go, defenderMaterial);
            ConfigureUnit(go, i);
            RegisterToBattleManager(go);
            spawnedUnits.Add(go);
        }
    }

    private void SpawnAttackers()
    {
        int n = envParams.AttackerCount;
        float x = envParams.AttackerSpawnX;
        float zMin = envParams.AttackerSpawnZMin;
        float zMax = envParams.AttackerSpawnZMax;

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0.5f : (float)i / (n - 1);
            float z = Mathf.Lerp(zMin, zMax, t);

            Vector3 pos = new Vector3(x, spawnY, z);

            // 初始朝向指向目标中心
            Vector3 dir = (envParams.TargetCenter - pos).normalized;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            GameObject go = Instantiate(attackerPrefab, pos, Quaternion.Euler(0f, yaw, 0f));
            go.name = $"Attacker_{i}";
            ApplyMaterial(go, attackerMaterial);
            ConfigureUnit(go, i);
            RegisterToBattleManager(go);
            spawnedUnits.Add(go);
        }
    }

    private void ApplyMaterial(GameObject go, Material mat)
    {
        if (go == null || mat == null) return;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rs.Length; i++)
        {
            rs[i].material = mat;
        }
    }

    private void ConfigureUnit(GameObject go, int id)
    {
        if (go == null || envParams == null) return;

        UavParams uavParams = go.GetComponent<UavParams>();
        if (uavParams != null)
        {
            uavParams.SetRuntimeId(id);
        }

        UavAgent agent = go.GetComponent<UavAgent>();
        if (agent != null)
        {
            agent.InitializeRuntimeContext(envParams, battleManager);
        }

        UavCombat combat = go.GetComponent<UavCombat>();
        if (combat != null)
        {
            combat.InitializeRuntimeContext(envParams, battleManager);
        }

        DubinsController dubinsController = go.GetComponent<DubinsController>();
        if (dubinsController != null)
        {
            dubinsController.InitializeRuntimeContext(envParams);
        }

        UavPositionOptimizer optimizer = go.GetComponent<UavPositionOptimizer>();
        if (optimizer != null)
        {
            optimizer.ResetTargetToCurrentPose();
        }
    }

    private void RegisterToBattleManager(GameObject go)
    {
        if (go == null || battleManager == null) return;

        UavParams uavParams = go.GetComponent<UavParams>();
        if (uavParams != null)
        {
            battleManager.Register(uavParams);
        }
    }
}
