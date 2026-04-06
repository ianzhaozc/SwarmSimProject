using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;

public class UavCombat : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private int currentHp = 15;
    public int CurrentHp => currentHp;

    [SerializeField] private bool isDead = false;
    public bool IsDead => isDead;

    private EnvParams envParams;
    private BattleManager battleManager;
    private UavParams selfParams;

    private float lastAttackTime = -999f;

    private float attackRadius = 60f;
    private float attackHalfAngleDeg = 45f;
    private float attackCooldown = 0.5f;
    private int damage = 1;
    private readonly List<UavParams> enemyBuffer = new List<UavParams>();

    public void InitializeRuntimeContext(EnvParams env, BattleManager manager)
    {
        envParams = env;
        battleManager = manager;
        if (envParams == null) return;

        attackRadius = envParams.AttackRadius;
        attackHalfAngleDeg = envParams.AttackHalfAngleDeg;
        attackCooldown = envParams.AttackCooldown;
        damage = envParams.AttackDamage;
        currentHp = envParams.MaxHp;
        isDead = false;
        lastAttackTime = -999f;
    }

    private void Awake()
    {
        selfParams = GetComponent<UavParams>();
    }

    private void Update()
    {
        if (isDead) return;
        if (selfParams == null) return;
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return;

        if (!CanAttackNow()) return;

        UavCombat target = FindBestAttackTarget();
        if (target == null) return;

        target.TakeDamage(damage);
        lastAttackTime = Time.time;
    }

    private bool CanAttackNow()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    private UavCombat FindBestAttackTarget()
    {
        battleManager.GetEnemyList(selfParams.Team, enemyBuffer);

        UavCombat bestTarget = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < enemyBuffer.Count; i++)
        {
            UavParams enemyParams = enemyBuffer[i];
            if (enemyParams == null) continue;

            UavCombat enemyCombat = enemyParams.GetComponent<UavCombat>();
            if (enemyCombat == null) continue;
            if (enemyCombat.IsDead) continue;

            if (!IsEnemyAttackable(enemyParams.transform))
                continue;

            float dist = FlatDistance(transform.position, enemyParams.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = enemyCombat;
            }
        }

        return bestTarget;
    }

    private bool IsEnemyAttackable(Transform enemy)
    {
        if (enemy == null) return false;

        Vector3 myPos = transform.position;
        Vector3 enemyPos = enemy.position;

        float dist = FlatDistance(myPos, enemyPos);
        if (dist > attackRadius) return false;

        return IsWithinAttackSector(enemyPos);
    }

    private bool IsWithinAttackSector(Vector3 enemyPos)
    {
        Vector3 forward = transform.forward;
        Vector3 forwardXZ = new Vector3(forward.x, 0f, forward.z);

        Vector3 dir = enemyPos - transform.position;
        Vector3 dirXZ = new Vector3(dir.x, 0f, dir.z);

        if (dirXZ.sqrMagnitude < 1e-6f) return true;

        float angle = Vector3.Angle(forwardXZ, dirXZ);
        return angle <= attackHalfAngleDeg;
    }

    public void TakeDamage(int value)
    {
        if (isDead) return;

        currentHp -= value;
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        DubinsController dubins = GetComponent<DubinsController>();
        if (dubins != null) dubins.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        DisableAgentControl();

        if (battleManager != null)
            battleManager.Unregister(selfParams);
    }

    private void DisableAgentControl()
    {
        UavAgent agent = GetComponent<UavAgent>();
        if (agent != null)
            agent.enabled = false;

        DecisionRequester decisionRequester = GetComponent<DecisionRequester>();
        if (decisionRequester != null)
            decisionRequester.enabled = false;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
