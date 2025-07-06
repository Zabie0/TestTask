using System.Collections;
using UnityEngine;

namespace Bubbles
{
    public class BubbleMover : MonoBehaviour
    {
        [SerializeField] private BubbleGridManager _gridManager;
        [SerializeField] private float _baseSpeed = 0.5f;
        [SerializeField] private float _speedMultiplier = 2.0f;
        [SerializeField] private float _minSpeed = 0.5f;
        [SerializeField] private int _maxRows = 30;
        
        private int _rows;
        private float _speed;

        public void Init(float speed)
        {
            _baseSpeed = speed;
            _speed = speed;
            StartCoroutine(MovingRoutine());
        }

        private IEnumerator MovingRoutine()
        {
            while (true)
            {
                UpdateSpeedBasedOnActiveRows();
                
                transform.Translate(Vector3.down * (_speed * Time.deltaTime));
                if (transform.localPosition.y < -_gridManager.VerticalSpacing * _rows-1)
                {
                    _gridManager.SpawnNewTopRow();
                    _rows++;
                }
                yield return null;
            }
        }

        private void UpdateSpeedBasedOnActiveRows()
        {
            int activeRows = GetActiveRowCount();
            
            float speedIncrease = (_maxRows - activeRows) * _speedMultiplier * _baseSpeed / _maxRows;
            float newSpeed = _baseSpeed + speedIncrease;
            
            _speed = Mathf.Max(newSpeed, _minSpeed);
        }

        private int GetActiveRowCount()
        {
            if (_gridManager.Grid.Count == 0) return 0;
            
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            
            foreach (var kvp in _gridManager.Grid)
            {
                minY = Mathf.Min(minY, kvp.Key.y);
                maxY = Mathf.Max(maxY, kvp.Key.y);
            }
            
            return maxY - minY + 1;
        }
    }
}