using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class GameEndHandler : MonoBehaviour
    {
        public static GameEndHandler Singleton { get; private set; }

        [SerializeField] private GameObject _endPanel;
        [SerializeField] private GameObject _darkBg;
        [SerializeField] private GameObject _mainScoreText;
        [SerializeField] private TMP_Text _panelScoreText;
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            Singleton = this;
            _restartButton.onClick.AddListener(Restart);
        }

        public void ShowEndScreen()
        {
            Time.timeScale = 0f;
            _mainScoreText.SetActive(false);
            _darkBg.SetActive(true);
            _endPanel.SetActive(true);
            _panelScoreText.text = $"Score: {ScoreHandler.Singleton.Score}";
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}