using UnityEngine;

public enum TeamType
{
    Attacker = 0,
    Defender = 1
}

public class UavParams : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private TeamType team = TeamType.Attacker;
    public TeamType Team => team;

    [SerializeField] private int uavId = -1;
    public int UavId => uavId;

    public void SetRuntimeId(int id)
    {
        uavId = id;
    }
}
