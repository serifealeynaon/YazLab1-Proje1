using UnityEngine;
using UnityEngine.UI;

namespace YazLab.UI
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text fpsText;

        private float smoothedDelta = 0.016f;

        private void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.05f);
            fpsText.text = $"FPS: {1f / smoothedDelta:F0}";
        }

        public void SetObjective(string text)
        {
            objectiveText.text = text;
        }

        public void SetStatus(string text)
        {
            statusText.text = text;
        }
    }
}
