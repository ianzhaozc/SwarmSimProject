using System.Collections.Generic;
using UnityEngine;

public class GameRuleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnvParams envParams;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleStatsManager battleStatsManager;

    [Header("Time Limit")]
    [SerializeField] private float maxEpisodeTime = 120f;

    [Header("Finish Control")]
    [SerializeField] private bool stopTimeOnFinish = false;

    [Header("Auto Episode Run")]
    [SerializeField] private bool autoRunEpisodes = true;
    [SerializeField] private int maxEpisodes = 10;
    [SerializeField] private float restartDelay = 1.0f;

    private float elapsed = 0f;
    private bool finished = false;
    private int currentEpisode = 1;
    private bool restartScheduled = false;
    private ScenarioManager scenarioManager;
    private readonly List<UavParams> attackerBuffer = new List<UavParams>();

    private void Awake()
    {
        envParams = envParams ?? GetComponent<EnvParams>() ?? FindObjectOfType<EnvParams>();
        battleManager = battleManager ?? GetComponent<BattleManager>() ?? FindObjectOfType<BattleManager>();
        scenarioManager = GetComponent<ScenarioManager>();

        if (battleStatsManager == null)
            battleStatsManager = GetComponent<BattleStatsManager>();

        if (battleStatsManager == null)
            battleStatsManager = gameObject.AddComponent<BattleStatsManager>();

        if (battleStatsManager != null)
            battleStatsManager.InitializeRuntimeContext(envParams, battleManager);
    }

    private void Start()
    {
        if (battleStatsManager != null)
        {
            battleStatsManager.ResetStats();
        }
    }

    private void Update()
    {
        if (finished) return;
        if (envParams == null) return;
        if (battleManager == null) return;

        elapsed += Time.deltaTime;

        if (CheckAttackerEnteredZone())
        {
            Finish("Attacker Win (Entered Zone)");
            return;
        }

        if (CheckAllAttackersDead())
        {
            Finish("Defender Win (All Attackers Dead)");
            return;
        }

        if (elapsed >= maxEpisodeTime)
        {
            Finish("Defender Win (Time Out)");
            return;
        }
    }

    private bool CheckAttackerEnteredZone()
    {
        battleManager.GetAliveUnits(TeamType.Attacker, attackerBuffer);
        Vector3 center = envParams.TargetCenter;
        float r = envParams.TargetRadius;

        for (int i = 0; i < attackerBuffer.Count; i++)
        {
            UavParams u = attackerBuffer[i];
            if (u == null) continue;

            Vector3 pos = u.transform.position;
            float dist = Vector3.Distance(
                new Vector3(pos.x, 0f, pos.z),
                new Vector3(center.x, 0f, center.z)
            );

            if (dist <= r)
                return true;
        }

        return false;
    }

    private bool CheckAllAttackersDead()
    {
        return battleManager.CountAliveUnits(TeamType.Attacker) == 0;
    }

    private void Finish(string result)
    {
        finished = true;
        Debug.Log($"[FINISH] episode={currentEpisode}, {result}, time={elapsed:F2}s");

        if (battleStatsManager != null)
        {
            battleStatsManager.OnBattleFinished(result, elapsed);
        }

        if (stopTimeOnFinish)
            Time.timeScale = 0f;

        if (autoRunEpisodes && currentEpisode < maxEpisodes && !restartScheduled)
        {
            restartScheduled = true;
            Invoke(nameof(RestartEpisode), restartDelay);
        }
    }

    private void RestartEpisode()
    {
        restartScheduled = false;
        currentEpisode++;

        if (stopTimeOnFinish)
            Time.timeScale = 1f;

        if (scenarioManager != null)
        {
            scenarioManager.SpawnAll();
        }

        ResetRuleState();
    }

    public void ResetRuleState()
    {
        elapsed = 0f;
        finished = false;

        if (stopTimeOnFinish)
            Time.timeScale = 1f;

        if (battleStatsManager != null)
        {
            battleStatsManager.ResetStats();
        }
    }
}
