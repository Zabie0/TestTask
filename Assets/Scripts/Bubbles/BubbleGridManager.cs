using System.Collections.Generic;
using UnityEngine;

namespace Bubbles
{
    public class BubbleGridManager : MonoBehaviour
    {
        public static BubbleGridManager Singleton { get; private set; }

        [SerializeField] private Transform _bubblesParent;
        [SerializeField] private BubbleSpawner _spawner;
        [SerializeField] private float _horizontalSpacing = 1.0f;
        [SerializeField] private float _verticalSpacing = 0.9f;
        public float VerticalSpacing => _verticalSpacing;
        
        private Dictionary<Vector2Int, Bubble> _grid = new();
        public Dictionary<Vector2Int, Bubble> Grid => _grid;
        
        private int _topMostSpawnedRow;

        private void Awake()
        {
            Singleton = this;
            _topMostSpawnedRow = 0;
        }
        
        public void SpawnNewTopRow()
        {
            var newRowY = _topMostSpawnedRow - 1;
            _spawner.SpawnRowAtWorldPosition(newRowY, GetStaticGridToWorld(newRowY));
            _topMostSpawnedRow = newRowY;
        }
        
        public void SetInitialTopRow(int topRow)
            => _topMostSpawnedRow = topRow;
        
        public void AddBubble(Vector2Int gridPos, Bubble bubble)
        {
            if (_grid.ContainsKey(gridPos)) return;
            bubble.gameObject.layer = 0;
            _grid[gridPos] = bubble;
            bubble.SetGridPosition(gridPos);
        }

        public void RemoveBubble(Vector2Int gridPos)
        {
            _grid.Remove(gridPos);
        }
        
        public Vector3 GridToWorldForExistingBubble(Vector2Int gridPos)
        {
            var neighborOffsets = GetNeighborOffsets(gridPos);
            
            foreach (var offset in neighborOffsets)
            {
                if (_grid.TryGetValue(offset, out var neighborBubble))
                {
                    return CalculateRelativeWorldPosition(
                        neighborBubble.transform.position, 
                        offset, 
                        gridPos
                    );
                }
            }
            
            return GridToWorld(gridPos);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            var isEvenRow = gridPos.y % 2 == 0;
            var xOffset = isEvenRow ? -_horizontalSpacing / 2f : 0f;
            
            var adjustedY = -gridPos.y * _verticalSpacing - _bubblesParent.localPosition.y;
            var local = new Vector3(gridPos.x * _horizontalSpacing + xOffset, adjustedY, 0f);
            return transform.TransformPoint(local);
        }
        
        public Vector2Int FindBestEmptyPositionNearBubble(Vector3 worldPos, Bubble referenceBubble)
        {
            var candidatePositions = new List<Vector2Int>();
            var neighborOffsets = GetNeighborOffsets(referenceBubble.GridPosition);
            
            foreach (var offset in neighborOffsets)
            {
                if (!_grid.ContainsKey(offset))
                {
                    candidatePositions.Add(offset);
                }
            }
            
            if (candidatePositions.Count == 0)
            {
                return referenceBubble.GridPosition;
            }

            var bestPosition = candidatePositions[0];
            var closestDistance = float.MaxValue;
            var referenceBubbleWorldPos = referenceBubble.transform.position;
            
            foreach (var pos in candidatePositions)
            {
                var relativeWorldPos = CalculateRelativeWorldPosition(referenceBubbleWorldPos, referenceBubble.GridPosition, pos);
                var distance = Vector3.Distance(worldPos, relativeWorldPos);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestPosition = pos;
                }
            }
            
            return bestPosition;
        }
        
        public List<Bubble> GetConnectedCluster(Vector2Int start, BubbleColor color)
        {
            List<Bubble> cluster = new();
            HashSet<Vector2Int> visited = new();
            Queue<Vector2Int> toCheck = new();
            
            if (!_grid.TryGetValue(start, out Bubble startBubble) || startBubble.Color != color)
                return cluster;
            
            toCheck.Enqueue(start);

            while (toCheck.Count > 0)
            {
                var pos = toCheck.Dequeue();
                if (!visited.Add(pos)) continue;

                if (_grid.TryGetValue(pos, out Bubble bubble) && bubble.Color == color)
                {
                    cluster.Add(bubble);
                    
                    foreach (var neighbor in GetNeighbors(pos))
                    {
                        if (neighbor.Color == color && !visited.Contains(neighbor.GridPosition))
                        {
                            toCheck.Enqueue(neighbor.GridPosition);
                        }
                    }
                }
            }

            return cluster;
        }

        public void DropDisconnectedBubbles()
        {
            if (_grid.Count == 0) return;
            
            var topY = _topMostSpawnedRow;

            HashSet<Vector2Int> connectedToTop = new();
            Queue<Vector2Int> toVisit = new();
            
            for (int checkRow = topY; checkRow <= topY + 2; checkRow++)
            {
                foreach (var kvp in _grid)
                {
                    if (kvp.Key.y == checkRow)
                    {
                        toVisit.Enqueue(kvp.Key);
                        connectedToTop.Add(kvp.Key);
                    }
                }
            }
            
            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (connectedToTop.Add(neighbor.GridPosition))
                    {
                        toVisit.Enqueue(neighbor.GridPosition);
                    }
                }
            }
            
            List<Vector2Int> toDrop = new();
            foreach (var kvp in _grid)
            {
                if (!connectedToTop.Contains(kvp.Key))
                    toDrop.Add(kvp.Key);
            }

            foreach (var pos in toDrop)
            {
                if (_grid.TryGetValue(pos, out var bubble))
                {
                    bubble.StartFalling();
                    _grid.Remove(pos);
                }
            }
        }
        
        private Vector3 GetStaticGridToWorld(int rowY)
        {
            var isEvenRow = rowY % 2 == 0;
            var xOffset = isEvenRow ? -_horizontalSpacing / 2f : 0f;
            
            var local = new Vector3(xOffset, 0f, 0f);
            return transform.TransformPoint(local);
        }

        private Vector2Int[] GetNeighborOffsets(Vector2Int pos)
        {
            var y = pos.y;
            var x = pos.x;
            
            if (y % 2 == 0) // Even rows
            {
                return new Vector2Int[]
                {
                    new(x-1, y),     // Left
                    new(x+1, y),     // Right
                    new(x-1, y-1),   // Top-left
                    new(x, y-1),     // Top-right
                    new(x-1, y+1),   // Bottom-left
                    new(x, y+1)      // Bottom-right
                };
            }
            
            return new Vector2Int[]
            {
                new(x-1, y),     // Left
                new(x+1, y),     // Right
                new(x, y-1),     // Top-left
                new(x+1, y-1),   // Top-right
                new(x, y+1),     // Bottom-left
                new(x+1, y+1)    // Bottom-right
            };
        }
        
        private Vector3 CalculateRelativeWorldPosition(Vector3 referenceBubbleWorldPos, Vector2Int referenceGridPos, Vector2Int targetGridPos)
        {
            var gridOffset = targetGridPos - referenceGridPos;
            
            var worldX = gridOffset.x * _horizontalSpacing;
            var worldY = -gridOffset.y * _verticalSpacing;
            
            if (referenceGridPos.y % 2 == 0 && targetGridPos.y % 2 != 0)
            {
                worldX += _horizontalSpacing / 2f;
            }
            else if (referenceGridPos.y % 2 != 0 && targetGridPos.y % 2 == 0)
            {
                worldX -= _horizontalSpacing / 2f;
            }
            
            return referenceBubbleWorldPos + new Vector3(worldX, worldY, 0f);
        }

        private List<Bubble> GetNeighbors(Vector2Int pos)
        {
            List<Bubble> neighbors = new();
            var neighborOffsets = GetNeighborOffsets(pos);

            foreach (var offset in neighborOffsets)
            {
                if (_grid.TryGetValue(offset, out Bubble bubble))
                    neighbors.Add(bubble);
            }

            return neighbors;
        }
    }
}
