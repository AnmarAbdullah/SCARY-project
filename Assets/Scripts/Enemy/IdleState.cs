using UnityEngine;

public class IdleState : State
{
    [Header("Idle Values")]
    [SerializeField, Min(0f)] private float duration = 2f;
    [SerializeField] private string animationName = "Idle";

    private float timer;

    public override void Enter()
    {
        timer = 0f;
        Debug.Log($"Entered Idle State. Animation: {animationName}");
    }

    public override void Run()
    {
        timer += Time.deltaTime;

        if (timer >= duration)
        {
            StateManager.ChangeState(StateManager.PatrolState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exited Idle State");
    }
}
