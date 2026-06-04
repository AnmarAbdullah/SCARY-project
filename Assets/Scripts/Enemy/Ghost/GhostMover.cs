using UnityEngine;
using UnityEngine.AI;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Thin wrapper around NavMeshAgent. States never touch the agent directly —
    /// they go through this. Single place to gate speed/destination changes.
    /// </summary>
    public class GhostMover : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float reachedTolerance = 0.4f;

        public NavMeshAgent Agent => agent;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
        }

        public void GoTo(Vector3 destination, float speed)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        public bool ReachedDestination()
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;
            return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, reachedTolerance);
        }

        public float CurrentSpeed => agent != null ? agent.velocity.magnitude : 0f;

        public void SetEnabled(bool on)
        {
            if (agent == null) return;
            agent.enabled = on;
        }

        public bool TrySampleRandomPoint(Vector3 center, float minRadius, float maxRadius, out Vector3 result)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 r = Random.insideUnitCircle * maxRadius;
                if (r.magnitude < minRadius)
                    r = r.normalized * minRadius;

                Vector3 candidate = center + new Vector3(r.x, 0f, r.y);
                if (NavMesh.SamplePosition(candidate, out var hit, 3f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = center;
            return false;
        }
    }
}
