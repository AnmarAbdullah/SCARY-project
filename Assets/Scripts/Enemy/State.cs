using UnityEngine;

public abstract class State : MonoBehaviour
{
    protected StateManager StateManager { get; private set; }

    public void Initialize(StateManager stateManager)
    {
        StateManager = stateManager;
    }

    public virtual void Enter()
    {
    }

    public abstract void Run();

    public virtual void Exit()
    {
    }
}
