using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProceduralWorld.Simulation.UI
{
    public class CursorManager : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private float tooltipOffset = 20f;

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void Update()
        {
            UpdateTooltipPosition();
        }

        public void ShowTooltip(string text, Vector3 worldPosition)
        {
            if (tooltipText != null && tooltipPanel != null)
            {
                tooltipText.text = text;
                tooltipPanel.SetActive(true);
                UpdateTooltipPosition();
            }
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void UpdateTooltipPosition()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                var mousePos = Input.mousePosition;
                tooltipPanel.transform.position = new Vector3(
                    mousePos.x + tooltipOffset,
                    mousePos.y + tooltipOffset,
                    0f
                );
            }
        }
    }
} 