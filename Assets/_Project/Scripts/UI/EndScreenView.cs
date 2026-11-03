using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YazLab.UI
{
    public class EndScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            panel.SetActive(false);
            restartButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
            quitButton.onClick.AddListener(Application.Quit);
        }

        public void Show(string summary)
        {
            summaryText.text = summary;
            panel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
