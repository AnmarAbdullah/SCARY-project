using UnityEngine;
using UnityEngine.AI;

public class ChaseState : State
{
    [Header("Chase Values")]
    [SerializeField] private float duration = 3f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform target;

    private float timer;

    public override void Enter()
    {
        timer = 0f;
        Debug.Log($"Entered Chase State. Speed: {moveSpeed}, Detection Range: {detectionRange}");
    }

    public override void Run()
    {
        agent.SetDestination(target.position);
    }

    public override void Exit()
    {
        Debug.Log("Exited Chase State");
    }
}
