namespace TimeFracture.Interfaces
{
    public interface IMovable
    {
        void Move(UnityEngine.Vector2 input, bool isSprinting);
        void Jump();
        void Crouch(bool isCrouching);
    }
}
