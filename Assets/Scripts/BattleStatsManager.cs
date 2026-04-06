using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BattleStatsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnvParams envParams;
    [SerializeField] private BattleManager battleManager;

    [Header("CSV Export")]
    [SerializeField] private bool exportCsv = true;
    [SerializeField] private string csvFileName = "battle_stats.csv";
    [SerializeField] private bool createSeparateFilePerRun = true;

    [Header("Runtime Distance Tracking")]
    private Dictionary<UavParams, Vector3> lastPositions = new Dictionary<UavParams, Vector3>();
    private Dictionary<UavParams, float> distanceAccum = new Dictionary<UavParams, float>();

    private bool battleFinished = false;
    private int episodeIndex = 0;
    private string csvFilePath;
    private readonly List<UavParams> aliveUnitsBuffer = new List<UavParams>();

    public void InitializeRuntimeContext(EnvParams env, BattleManager battle)
    {
        if (env != null)
            envParams = env;
        if (battle != null)
            battleManager = battle;
    }

    private void Awake()
    {
        ResolveMissingReferences();

        string pythonDir = Path.Combine(Directory.GetCurrentDirectory(), "Python");
        string resultsDir = Path.Combine(pythonDir, "Results");

        if (!Directory.Exists(resultsDir))
        {
            Directory.CreateDirectory(resultsDir);
        }

        csvFilePath = BuildCsvFilePath(resultsDir);

        if (exportCsv && !File.Exists(csvFilePath))
        {
            WriteCsvHeader();
        }

        if (exportCsv)
        {
            Debug.Log($"[CSV] Export file initialized: {csvFilePath}");
        }
    }

    private void Update()
    {
        if (battleFinished) return;
        ResolveMissingReferences();
        if (battleManager == null) return;

        TrackDistances();
    }

    public void ResetStats()
    {
        battleFinished = false;
        lastPositions.Clear();
        distanceAccum.Clear();
        episodeIndex++;

        if (battleManager == null) return;

        battleManager.GetAllAliveUnits(aliveUnitsBuffer);
        for (int i = 0; i < aliveUnitsBuffer.Count; i++)
        {
            UavParams unit = aliveUnitsBuffer[i];
            if (unit == null) continue;

            lastPositions[unit] = unit.transform.position;
            distanceAccum[unit] = 0f;
        }
    }

    private void TrackDistances()
    {
        battleManager.GetAllAliveUnits(aliveUnitsBuffer);

        for (int i = 0; i < aliveUnitsBuffer.Count; i++)
        {
            UavParams unit = aliveUnitsBuffer[i];
            if (unit == null) continue;

            Vector3 currentPos = unit.transform.position;

            if (!lastPositions.ContainsKey(unit))
            {
                lastPositions[unit] = currentPos;
                distanceAccum[unit] = 0f;
                continue;
            }

            float delta = FlatDistance(lastPositions[unit], currentPos);
            distanceAccum[unit] += delta;
            lastPositions[unit] = currentPos;
        }
    }

    public void OnBattleFinished(string result, float elapsedTime)
    {
        if (battleFinished) return;

        battleFinished = true;

        int attackerAlive = (battleManager != null) ? battleManager.GetAliveAttackerCount() : 0;
        int defenderAlive = (battleManager != null) ? battleManager.GetAliveDefenderCount() : 0;

        int attackerTotal = (envParams != null) ? envParams.AttackerCount : 0;
        int defenderTotal = (envParams != null) ? envParams.DefenderCount : 0;

        float attackerSurvivalRate = (attackerTotal > 0) ? (float)attackerAlive / attackerTotal : 0f;
        float defenderSurvivalRate = (defenderTotal > 0) ? (float)defenderAlive / defenderTotal : 0f;

        float attackerTotalDistance = GetTeamTotalDistance(TeamType.Attacker);
        float defenderTotalDistance = GetTeamTotalDistance(TeamType.Defender);

        Debug.Log(
            "[STATS] " +
            $"episode={episodeIndex}, " +
            $"winner={result}, " +
            $"time={elapsedTime:F2}, " +
            $"attacker_alive={attackerAlive}/{attackerTotal}, " +
            $"defender_alive={defenderAlive}/{defenderTotal}, " +
            $"attacker_survival_rate={attackerSurvivalRate:F2}, " +
            $"defender_survival_rate={defenderSurvivalRate:F2}, " +
            $"attacker_total_distance={attackerTotalDistance:F2}, " +
            $"defender_total_distance={defenderTotalDistance:F2}"
        );

        if (exportCsv)
        {
            AppendCsvRow(
                episodeIndex,
                result,
                elapsedTime,
                attackerAlive,
                defenderAlive,
                attackerSurvivalRate,
                defenderSurvivalRate,
                attackerTotalDistance,
                defenderTotalDistance
            );

            Debug.Log($"[CSV] Saved to: {csvFilePath}");
        }
    }

    public float GetTeamTotalDistance(TeamType team)
    {
        float total = 0f;

        foreach (var kv in distanceAccum)
        {
            UavParams unit = kv.Key;
            if (unit == null) continue;
            if (unit.Team != team) continue;

            total += kv.Value;
        }

        return total;
    }

    private void WriteCsvHeader()
    {
        string header =
            "episode,winner,time,attacker_alive,defender_alive," +
            "attacker_survival_rate,defender_survival_rate," +
            "attacker_total_distance,defender_total_distance";

        File.WriteAllText(csvFilePath, header + "\n");
    }

    private void AppendCsvRow(
        int episode,
        string winner,
        float time,
        int attackerAlive,
        int defenderAlive,
        float attackerSurvivalRate,
        float defenderSurvivalRate,
        float attackerTotalDistance,
        float defenderTotalDistance
    )
    {
        string safeWinner = winner.Replace(",", " ");
        string row =
            $"{episode},{safeWinner},{time:F2},{attackerAlive},{defenderAlive}," +
            $"{attackerSurvivalRate:F4},{defenderSurvivalRate:F4}," +
            $"{attackerTotalDistance:F2},{defenderTotalDistance:F2}";

        File.AppendAllText(csvFilePath, row + "\n");
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void ResolveMissingReferences()
    {
        if (envParams == null)
            envParams = GetComponent<EnvParams>() ?? FindObjectOfType<EnvParams>();

        if (battleManager == null)
            battleManager = GetComponent<BattleManager>() ?? FindObjectOfType<BattleManager>();
    }

    private string BuildCsvFilePath(string resultsDir)
    {
        string configuredName = string.IsNullOrWhiteSpace(csvFileName) ? "battle_stats.csv" : csvFileName.Trim();
        string extension = Path.GetExtension(configuredName);
        if (string.IsNullOrEmpty(extension))
            extension = ".csv";

        string baseName = Path.GetFileNameWithoutExtension(configuredName);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "battle_stats";

        string resolvedFileName = configuredName;
        if (createSeparateFilePerRun)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            resolvedFileName = $"{baseName}_{timestamp}{extension}";
        }

        return Path.Combine(resultsDir, resolvedFileName);
    }
}
