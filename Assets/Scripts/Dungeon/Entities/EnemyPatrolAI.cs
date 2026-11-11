using System.Collections.Generic;
using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Entities
{
    /// <summary>
    /// Autonomous wandering enemy AI with patrol waypoints and smooth locomotion.
    /// </summary>
    public class EnemyPatrolAI : MonoBehaviour
    {
        #region CONFIG
        public static class ENEMY_CONFIG
        {
            // Locomotion values
            public const float PATROL_SPEED = 2.5f;
            public const float ROTATION_SPEED = 8.0f;
            public const float WAYPOINT_ARRIVAL_DISTANCE = 0.35f;

            // Idle timer values
            public const float MIN_IDLE_TIME = 1.0f;
            public const float MAX_IDLE_TIME = 3.0f;
        }
        #endregion

        private DungeonGrid _grid;
        private Vector3 _currentDestination;
        private float _idleTimer;
        private bool _isIdle;
        private readonly List<Vector2Int> _localPatrolCells = new List<Vector2Int>();

        public void Initialize(DungeonGrid grid, Vector2Int spawnCell, int patrolRadius = 3)
        {
            _grid = grid;
            _localPatrolCells.Clear();

            // Find surrounding walkable cells to patrol
            for (int dx = -patrolRadius; dx <= patrolRadius; dx++)
            {
                for (int dy = -patrolRadius; dy <= patrolRadius; dy++)
                {
                    Vector2Int cell = new Vector2Int(spawnCell.x + dx, spawnCell.y + dy);
                    if (_grid != null && _grid.InBounds(cell) && _grid.IsWalkable(cell))
                    {
                        _localPatrolCells.Add(cell);
                    }
                }
            }

            if (_localPatrolCells.Count == 0)
            {
                _localPatrolCells.Add(spawnCell);
            }

            ChooseNewWaypoint();
        }

        private void Update()
        {
            if (_grid == null || _localPatrolCells.Count == 0) return;

            if (_isIdle)
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0.0f)
                {
                    _isIdle = false;
                    ChooseNewWaypoint();
                }
                return;
            }

            // Move toward waypoint
            Vector3 targetDir = (_currentDestination - transform.position);
            targetDir.y = 0.0f;

            if (targetDir.magnitude <= ENEMY_CONFIG.WAYPOINT_ARRIVAL_DISTANCE)
            {
                _isIdle = true;
                _idleTimer = Random.Range(ENEMY_CONFIG.MIN_IDLE_TIME, ENEMY_CONFIG.MAX_IDLE_TIME);
            }
            else
            {
                Vector3 moveDelta = targetDir.normalized * (ENEMY_CONFIG.PATROL_SPEED * Time.deltaTime);
                transform.position += moveDelta;

                if (targetDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetDir.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, ENEMY_CONFIG.ROTATION_SPEED * Time.deltaTime);
                }
            }
        }

        private void ChooseNewWaypoint()
        {
            if (_localPatrolCells.Count == 0) return;

            int randomIndex = Random.Range(0, _localPatrolCells.Count);
            Vector2Int chosenCell = _localPatrolCells[randomIndex];
            _currentDestination = _grid.GridToWorldPosition(chosenCell, transform.position.y);
        }
    }
}
