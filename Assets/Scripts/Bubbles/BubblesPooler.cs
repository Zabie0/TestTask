using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bubbles
{
    public class BubblesPooler : MonoBehaviour
    {
        public static BubblesPooler Singleton { get; private set; }

        [SerializeField] private Bubble _bubblePrefab;

        private Dictionary<Bubble, bool> _pool = new();
        
        private void Awake()
            => Singleton = this;

        public Bubble Get()
        {
            if (!_pool.ContainsValue(true)) 
                return CreateNew();
            
            var bubblePair = _pool.FirstOrDefault(x => x.Value);
            _pool[bubblePair.Key] = false;
            bubblePair.Key.gameObject.SetActive(true);
            return bubblePair.Key;
        }
        
        public void ReturnToPool(Bubble bubble)
        {
            bubble.transform.position = transform.position;
            bubble.transform.rotation = Quaternion.identity;
            bubble.gameObject.SetActive(false);
            _pool[bubble] = true;
        }

        private Bubble CreateNew()
        {
            var bubble = Instantiate(_bubblePrefab, Vector3.zero, Quaternion.identity);
            _pool.Add(bubble, false);
            return bubble;
        }
    }
}