using UnityEngine;

[RequireComponent(typeof(IdleState), typeof(PatrolState), typeof(ChaseState))]
public class StateManager : MonoBehaviour
{
    [Header("Start State")]
    [SerializeField] private State startingState;

    [Header("State References")]
    [SerializeField] private IdleState idleState;
    [SerializeField] private PatrolState patrolState;
    [SerializeField] private ChaseState chaseState;

    public State CurrentState { get; private set; }
    public IdleState IdleState => idleState;
    public PatrolState PatrolState => patrolState;
    public ChaseState ChaseState => chaseState;

    private void Awake()
    {
        FindMissingStateReferences();
        InitializeStates();
    }

    private void OnValidate()
    {
        FindMissingStateReferences();

        if (startingState == null)
        {
            startingState = idleState;
        }
    }

    private void Start()
    {
        ChangeState(startingState != null ? startingState : idleState);
    }

    private void Update()
    {
        CurrentState?.Run();
    }

    public void ChangeState(State nextState)
    {
        if (nextState == null)
        {
            Debug.LogWarning("Tried to change to a missing state.", this);
            return;
        }

        if (nextState == CurrentState)
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    private void FindMissingStateReferences()
    {
        idleState ??= GetComponent<IdleState>();
        patrolState ??= GetComponent<PatrolState>();
        chaseState ??= GetComponent<ChaseState>();
    }

    private void InitializeStates()
    {
        idleState?.Initialize(this);
        patrolState?.Initialize(this);
        chaseState?.Initialize(this);
    }
}
