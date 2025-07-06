using TMPro;
using UnityEngine;

namespace UI
{
    public class ScoreHandler : MonoBehaviour
    {
        public static ScoreHandler Singleton { get; private set; }

        [SerializeField] private TMP_Text _scoreText;
        private int _score;
        public int Score => _score;

        private void Awake()
            => Singleton = this;

        public void AddScore(int score)
        {
            _score += score;
            _scoreText.text = $"{_score}";
        }
    }
}