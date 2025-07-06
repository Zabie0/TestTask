using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Bubbles
{
    public class BubbleShooter : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Transform _bubblesParent;
        [SerializeField] private Transform _slopeTransform;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Bubble _bubblePrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private LayerMask _aimPlaneLayerMask;

        [Header("Settings")]
        [SerializeField] private float _maxLineLength = 30f;
        [SerializeField] private float _bubbleSpeed = 10f;
        [SerializeField] private float _bouncePadding = 0.01f;
        [SerializeField] private int _maxBounces = 3;

        private Camera _camera;
        private bool _isDragging;
        private Bubble _currentBubble;
        private bool _canShoot = true;

        private void Start()
        {
            _camera = Camera.main;
            SpawnNewBubble();
        }

        private void Update()
        {
            if (!_canShoot || Input.touchCount <= 0) return;
            
            var touch = Input.GetTouch(0);
            HandleTouchInput(touch);
        }

        private void HandleTouchInput(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartAiming();
                    break;
                    
                case TouchPhase.Moved:
                    if (_isDragging)
                    {
                        Vector3 worldDir = GetSlopeDirectionFromTouch(touch.position);
                        UpdateAimingLine(worldDir);
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isDragging)
                    {
                        FireBubble(touch.position);
                        StopAiming();
                    }
                    break;
            }
        }

        private void StartAiming()
        {
            _isDragging = true;
            _lineRenderer.enabled = true;
        }

        private void StopAiming()
        {
            _isDragging = false;
            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }

        private void UpdateAimingLine(Vector3 direction)
        {
            if (!_spawnPoint) return;
            DrawAimingLine(_spawnPoint.position, direction);
        }

        private void FireBubble(Vector3 screenPoint)
        {
            if (!_currentBubble || !_canShoot) return;

            var worldDir = GetSlopeDirectionFromTouch(screenPoint);
            _canShoot = false;
            
            _currentBubble.SetupForLaunch(worldDir, _bubbleSpeed);
            _currentBubble.OnBubbleCollision += OnBubbleCollision;
            
            _currentBubble = null;
        }

        private void OnBubbleCollision(Bubble launchedBubble, Bubble hitBubble)
        {
            launchedBubble.OnBubbleCollision -= OnBubbleCollision;
            StartCoroutine(HandleBubbleCollision(launchedBubble, hitBubble));
        }

        private IEnumerator HandleBubbleCollision(Bubble launchedBubble, Bubble hitBubble)
        {
            launchedBubble.StopLaunch();
            
            var targetGridPos = BubbleGridManager.Singleton.FindBestEmptyPositionNearBubble(
                launchedBubble.transform.position, 
                hitBubble
            );
            
            var targetWorldPos = BubbleGridManager.Singleton.GridToWorldForExistingBubble(targetGridPos);
            PositionBubbleInGrid(launchedBubble, targetGridPos, targetWorldPos);
            
            yield return StartCoroutine(ProcessGameLogic(launchedBubble));
            
            PrepareForNextShot();
        }

        private void PositionBubbleInGrid(Bubble bubble, Vector2Int gridPos, Vector3 worldPos)
        {
            bubble.transform.SetParent(_bubblesParent, true);
            bubble.transform.position = worldPos;
            bubble.transform.localPosition = new Vector3(bubble.transform.localPosition.x, bubble.transform.localPosition.y - 0.2f, 0f);
            bubble.transform.rotation = BubbleGridManager.Singleton.transform.rotation;
            
            BubbleGridManager.Singleton.AddBubble(gridPos, bubble);
            
            bubble.SetGridPosition(gridPos);
        }
        
        private IEnumerator ProcessGameLogic(Bubble placedBubble)
        {
            var cluster = BubbleGridManager.Singleton.GetConnectedCluster(placedBubble.GridPosition, placedBubble.Color);
            
            if (cluster.Count >= 3)
            {
                foreach (var bubble in cluster)
                {
                    BubbleGridManager.Singleton.RemoveBubble(bubble.GridPosition);
                    bubble.StartFalling();
                }
                BubbleGridManager.Singleton.DropDisconnectedBubbles();
                
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        private void PrepareForNextShot()
        {
            SpawnNewBubble();
            _canShoot = true;
        }
        
        private Vector3 GetSlopeDirectionFromTouch(Vector2 screenPos)
        {
            var ray = _camera.ScreenPointToRay(screenPos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _aimPlaneLayerMask))
            {
                Vector3 direction = (hit.point - _spawnPoint.position).normalized;
                return direction;
            }
            
            return _slopeTransform.up;
        }

        private void DrawAimingLine(Vector3 startPos, Vector3 direction)
        {
            var points = new List<Vector3> { startPos };
            var currentPos = startPos;
            var currentDir = direction;
            var remainingLength = _maxLineLength;

            for (int bounce = 0; bounce < _maxBounces && remainingLength > 0; bounce++)
            {
                var ray = new Ray(currentPos, currentDir);
                
                if (Physics.Raycast(ray, out RaycastHit hit, remainingLength, _collisionMask))
                {
                    var hitPoint = hit.point - currentDir * _bouncePadding;
                    points.Add(hitPoint);
                    
                    currentDir = Vector3.Reflect(currentDir, hit.normal);
                    currentPos = hitPoint;
                    
                    remainingLength -= Vector3.Distance(points[^2], hitPoint);
                }
                else
                {
                    points.Add(currentPos + currentDir * remainingLength);
                    break;
                }
            }
            
            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());
        }

        private void SpawnNewBubble()
        {
            _currentBubble = Instantiate(_bubblePrefab, _spawnPoint.position, Quaternion.identity);
            
            var randomColor = GetRandomBubbleColor();
            _currentBubble.Init(randomColor);
            
            _currentBubble.gameObject.layer = 9;
        }

        private BubbleColor GetRandomBubbleColor()
        {
            var existingColors = new HashSet<BubbleColor>();
            
            if (BubbleGridManager.Singleton.Grid.Count > 0)
            {
                foreach (var bubble in BubbleGridManager.Singleton.Grid.Values)
                {
                    existingColors.Add(bubble.Color);
                }
            }
            
            if (existingColors.Count > 0 && Random.Range(0f, 1f) < 0.8f)
            {
                var colorsArray = new BubbleColor[existingColors.Count];
                existingColors.CopyTo(colorsArray);
                return colorsArray[Random.Range(0, colorsArray.Length)];
            }
            
            var allColors = System.Enum.GetValues(typeof(BubbleColor));
            return (BubbleColor)allColors.GetValue(Random.Range(0, allColors.Length));
        }
    }
}
