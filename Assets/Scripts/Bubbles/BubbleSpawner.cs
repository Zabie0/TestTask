using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bubbles
{
    public class BubbleSpawner : MonoBehaviour
    {
        [SerializeField] private BubbleMover _bubblesMover;
        [SerializeField] private Bubble _bubblePrefab;
        [SerializeField] private int _rows = 10;
        [SerializeField] private int _columns = 6;
        [SerializeField] private float _horizontalSpacing = 1.0f;
        [SerializeField] private float _fallSpeed = 0.5f;

        private void Start()
        {
            SpawnInitialGrid();
            BubbleGridManager.Singleton.SetInitialTopRow(0);
            _bubblesMover.Init(_fallSpeed);
        }
        
        public void SpawnRowAtWorldPosition(int rowY, Vector3 baseWorldPos)
        {
            var isEvenRow = rowY % 2 == 0;
            var colsThisRow = isEvenRow ? _columns + 1 : _columns;

            for (int x = 0; x < colsThisRow; x++)
            {
                var gridPos = new Vector2Int(x, rowY);
                
                var xOffset = x * _horizontalSpacing;
                var worldPos = baseWorldPos + new Vector3(xOffset, 0f, 0f);

                var bubble = BubblesPooler.Singleton.Get();
                
                bubble.transform.SetParent(_bubblesMover.transform);
                bubble.transform.rotation = transform.rotation;
                bubble.transform.position = worldPos;
                
                var color = GetRandomColor();
                bubble.Init(color);

                BubbleGridManager.Singleton.AddBubble(gridPos, bubble);
            }
        }

        private void SpawnInitialGrid()
        {
            for (int y = 0; y < _rows; y++)
            {
                SpawnRow(y);
            }
        }

        private void SpawnRow(int rowY)
        {
            var isEvenRow = rowY % 2 == 0;
            var colsThisRow = isEvenRow ? _columns + 1 : _columns;
            
            for (int x = 0; x < colsThisRow; x++)
            {
                var gridPos = new Vector2Int(x, rowY);
                var worldPos = BubbleGridManager.Singleton.GridToWorld(gridPos);

                var bubble = BubblesPooler.Singleton.Get();
                
                bubble.transform.SetParent(_bubblesMover.transform);
                bubble.transform.rotation = transform.rotation;
                bubble.transform.position = worldPos;
                
                var color = GetRandomColor();
                bubble.Init(color);

                BubbleGridManager.Singleton.AddBubble(gridPos, bubble);
            }
        }

        private BubbleColor GetRandomColor()
        {
            var values = Enum.GetValues(typeof(BubbleColor));
            return (BubbleColor)values.GetValue(Random.Range(0, values.Length));
        }
    }
}