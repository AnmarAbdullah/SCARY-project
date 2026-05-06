using UnityEngine;

public class PatrolState : State
{
    [Header("Patrol Values")]
    [SerializeField, Min(0f)] private float duration = 4f;
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float patrolRadius = 8f;

    private float timer;

    public override void Enter()
    {
        timer = 0f;
        Debug.Log($"Entered Patrol State. Speed: {moveSpeed}, Radius: {patrolRadius}");
    }

    public override void Run()
    {
        timer += Time.deltaTime;

        if (timer >= duration)
        {
            StateManager.ChangeState(StateManager.ChaseState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exited Patrol State");
    }
}
