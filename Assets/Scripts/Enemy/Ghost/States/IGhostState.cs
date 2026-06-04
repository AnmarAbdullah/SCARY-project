namespace ScaryGame.Enemy
{
    public interface IGhostState
    {
        void Enter();
        void Run(float dt);
        void Exit();
    }
}
