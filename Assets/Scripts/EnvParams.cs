using UnityEngine;

public class EnvParams : MonoBehaviour
{
    [Header("Battlefield")]
    [SerializeField] private float areaSize = 1000f;          // 战场边长
    public float AreaSize => areaSize;
    public float HalfAreaSize => areaSize * 0.5f;

    [Header("Target Zone")]
    [SerializeField] private Vector3 targetCenter = Vector3.zero;
    public Vector3 TargetCenter => targetCenter;

    [SerializeField] private float targetRadius = 75f;        // 目标区半径
    public float TargetRadius => targetRadius;

    [Header("Global Detection Around Target")]
    [SerializeField] private bool enableGlobalDetection = true;   // 目标区共享探测开关
    public bool EnableGlobalDetection => enableGlobalDetection;

    [SerializeField] private float globalDetectRadius = 300f;     // 目标区外圈探测半径
    public float GlobalDetectRadius => globalDetectRadius;

    [Header("Local Sensing")]
    [SerializeField] private float localSenseRadius = 150f;       // 无人机局部感知半径
    public float LocalSenseRadius => localSenseRadius;

    [Header("Combat")]
    [SerializeField] private float attackRadius = 60f;            // 攻击距离
    public float AttackRadius => attackRadius;

    [SerializeField] private float attackHalfAngleDeg = 45f;      // 机头左右45°
    public float AttackHalfAngleDeg => attackHalfAngleDeg;

    [SerializeField] private float attackCooldown = 0.5f;
    public float AttackCooldown => attackCooldown;

    [SerializeField] private int maxHp = 15;
    public int MaxHp => maxHp;

    [SerializeField] private int attackDamage = 1;
    public int AttackDamage => attackDamage;

    [Header("Motion")]
    [SerializeField] private float speed = 15f;
    public float Speed => speed;

    [SerializeField] private float minTurnRadius = 35f;
    public float MinTurnRadius => minTurnRadius;

    [Header("Teams")]
    [SerializeField] private int attackerCount = 5;
    public int AttackerCount => attackerCount;

    [SerializeField] private int defenderCount = 5;
    public int DefenderCount => defenderCount;

    [Header("Defender Patrol")]
    [SerializeField] private float defenderPatrolRadius = 150f;
    public float DefenderPatrolRadius => defenderPatrolRadius;

    [Header("Attacker Spawn Line")]
    [SerializeField] private float attackerSpawnX = 480f;
    public float AttackerSpawnX => attackerSpawnX;

    [SerializeField] private float attackerSpawnZMin = -300f;
    public float AttackerSpawnZMin => attackerSpawnZMin;

    [SerializeField] private float attackerSpawnZMax = 300f;
    public float AttackerSpawnZMax => attackerSpawnZMax;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetCenter, new Vector3(areaSize, 0.1f, areaSize));

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetCenter, targetRadius);

        if (enableGlobalDetection)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetCenter, globalDetectRadius);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(targetCenter, defenderPatrolRadius);
    }
}