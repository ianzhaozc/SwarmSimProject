using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Runtime Lists")]
    [SerializeField] private List<UavParams> attackers = new List<UavParams>();
    [SerializeField] private List<UavParams> defenders = new List<UavParams>();

    public IReadOnlyList<UavParams> Attackers => attackers;
    public IReadOnlyList<UavParams> Defenders => defenders;

    public void ClearAll()
    {
        attackers.Clear();
        defenders.Clear();
    }

    public void Register(UavParams unit)
    {
        if (unit == null) return;

        if (unit.Team == TeamType.Attacker)
        {
            if (!attackers.Contains(unit))
                attackers.Add(unit);
        }
        else if (unit.Team == TeamType.Defender)
        {
            if (!defenders.Contains(unit))
                defenders.Add(unit);
        }
    }

    public void Unregister(UavParams unit)
    {
        if (unit == null) return;

        if (unit.Team == TeamType.Attacker)
            attackers.Remove(unit);
        else if (unit.Team == TeamType.Defender)
            defenders.Remove(unit);
    }

    public void GetAliveUnits(TeamType team, List<UavParams> results)
    {
        if (results == null) return;

        results.Clear();
        FillAliveUnits(team == TeamType.Attacker ? attackers : defenders, results);
    }

    public void GetEnemyList(TeamType selfTeam, List<UavParams> results)
    {
        GetAliveUnits(selfTeam == TeamType.Attacker ? TeamType.Defender : TeamType.Attacker, results);
    }

    public void GetFriendlyList(TeamType selfTeam, List<UavParams> results)
    {
        GetAliveUnits(selfTeam, results);
    }

    public void GetAllAliveUnits(List<UavParams> results)
    {
        if (results == null) return;

        results.Clear();
        FillAliveUnits(attackers, results);
        FillAliveUnits(defenders, results);
    }

    public List<UavParams> GetAliveAttackers()
    {
        List<UavParams> alive = new List<UavParams>();
        FillAliveUnits(attackers, alive);
        return alive;
    }

    public List<UavParams> GetAliveDefenders()
    {
        List<UavParams> alive = new List<UavParams>();
        FillAliveUnits(defenders, alive);
        return alive;
    }

    public int GetAliveAttackerCount()
    {
        return CountAliveUnits(TeamType.Attacker);
    }

    public int GetAliveDefenderCount()
    {
        return CountAliveUnits(TeamType.Defender);
    }

    public int CountAliveUnits(TeamType team)
    {
        List<UavParams> source = team == TeamType.Attacker ? attackers : defenders;
        int aliveCount = 0;

        for (int i = 0; i < source.Count; i++)
        {
            if (IsAlive(source[i]))
                aliveCount++;
        }

        return aliveCount;
    }

    public List<UavParams> GetEnemyList(TeamType selfTeam)
    {
        return (selfTeam == TeamType.Attacker) ? GetAliveDefenders() : GetAliveAttackers();
    }

    public List<UavParams> GetFriendlyList(TeamType selfTeam)
    {
        return (selfTeam == TeamType.Attacker) ? GetAliveAttackers() : GetAliveDefenders();
    }

    public List<UavParams> GetAllAliveUnits()
    {
        List<UavParams> all = new List<UavParams>();
        GetAllAliveUnits(all);
        return all;
    }

    public UavParams GetClosestAliveEnemy(UavParams self)
    {
        if (self == null) return null;

        List<UavParams> enemies = GetEnemyList(self.Team);
        UavParams best = null;
        float bestDist = float.MaxValue;

        Vector3 selfPos = self.transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            UavParams enemy = enemies[i];
            if (enemy == null) continue;

            float dist = FlatDistance(selfPos, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }

        return best;
    }

    public List<UavParams> GetAliveEnemiesInRange(UavParams self, float radius)
    {
        List<UavParams> result = new List<UavParams>();
        if (self == null) return result;

        List<UavParams> enemies = GetEnemyList(self.Team);
        Vector3 selfPos = self.transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            UavParams enemy = enemies[i];
            if (enemy == null) continue;

            float dist = FlatDistance(selfPos, enemy.transform.position);
            if (dist <= radius)
                result.Add(enemy);
        }

        return result;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void FillAliveUnits(List<UavParams> source, List<UavParams> results)
    {
        for (int i = 0; i < source.Count; i++)
        {
            UavParams unit = source[i];
            if (!IsAlive(unit)) continue;

            results.Add(unit);
        }
    }

    private bool IsAlive(UavParams unit)
    {
        if (unit == null) return false;

        UavCombat combat = unit.GetComponent<UavCombat>();
        return combat != null && !combat.IsDead;
    }
}
