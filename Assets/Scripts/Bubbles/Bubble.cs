using System.Collections;
using UnityEngine;
using System;
using DG.Tweening;
using UI;

namespace Bubbles
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class Bubble : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private int _scoreValue = 10;
        [SerializeField] private float _despawnHeight = -10f;
        
        [Header("References")]
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Rigidbody _rb;
        
        [Header("Launch Settings")]
        [SerializeField] private float _bounceForce = 0.8f;
        [SerializeField] private LayerMask _wallLayerMask = -1;
        
        public event Action<Bubble, Bubble> OnBubbleCollision;
        
        private BubbleColor _bubbleColor;
        public BubbleColor Color => _bubbleColor;
        private bool _isLaunched;
        private bool _isFalling;
        private Vector3 _launchDirection;
        private float _launchSpeed;

        public void Init(BubbleColor color)
        {
            _bubbleColor = color;
            _renderer.material.color = GetColorTint(color);
            _rb.isKinematic = true;
            _isFalling = false;
        }
        
        public void SetGridPosition(Vector2Int gridPos)
            => GridPosition = gridPos;

        public void SetupForLaunch(Vector3 direction, float speed)
        {
            _isLaunched = true;
            _launchDirection = direction.normalized;
            _launchSpeed = speed;
            
            _rb.isKinematic = false;
            _rb.useGravity = false;
            _rb.velocity = _launchDirection * _launchSpeed;
        }
        
        public void StopLaunch()
        {
            _isLaunched = false;
            _rb.velocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        
        public void StartFalling()
        {
            _isLaunched = false;
            _isFalling = true;
            
            transform.SetParent(null);
            _rb.isKinematic = false;
            _rb.useGravity = true;
            gameObject.layer = 8;
            
            transform.DOLocalMoveY(transform.localPosition.y+0.5f, 0.1f)
                .OnComplete(() =>
                {
                    _rb.velocity = Vector3.down * 3f;
                    StartCoroutine(FallingRoutine());
                });
        }

        private IEnumerator FallingRoutine()
        {
            while (transform.position.y > _despawnHeight)
            {
                yield return null;
            }
            ScoreHandler.Singleton.AddScore(_scoreValue);
            BubblesPooler.Singleton.ReturnToPool(this);
        }
        
        private void HandleWallBounce(Collision collision)
        {
            var normal = collision.contacts[0].normal;
            var reflectedDirection = Vector3.Reflect(_launchDirection, normal);
            _launchDirection = reflectedDirection.normalized;
            _rb.velocity = _launchDirection * _launchSpeed * _bounceForce;
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
            => (layerMask.value & (1 << layer)) != 0;
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!_isLaunched) return;
            
            var otherBubble = collision.gameObject.GetComponent<Bubble>();
            if (otherBubble && otherBubble != this)
            {
                OnBubbleCollision?.Invoke(this, otherBubble);
                return;
            }
            
            if (IsInLayerMask(collision.gameObject.layer, _wallLayerMask))
            {
                HandleWallBounce(collision);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isFalling || _isLaunched) return;
            if (other.CompareTag("FailLine"))
            {
                GameEndHandler.Singleton.ShowEndScreen();
            }
        }

        private Color GetColorTint(BubbleColor color)
        {
            return color switch
            {
                BubbleColor.Red => UnityEngine.Color.red,
                BubbleColor.Blue => UnityEngine.Color.blue,
                BubbleColor.Green => UnityEngine.Color.green,
                BubbleColor.Yellow => UnityEngine.Color.yellow,
                _ => UnityEngine.Color.red
            };
        }
    }
}