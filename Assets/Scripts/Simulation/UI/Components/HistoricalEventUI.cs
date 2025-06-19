using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.UI.Components
{
    public class HistoricalEventUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI yearText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button selectButton;

        private HistoricalEventRecord _event;
        public System.Action<HistoricalEventRecord> OnEventSelected;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelect);
            }
        }

        public void Initialize(HistoricalEventRecord evt)
        {
            _event = evt;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (titleText != null)
                titleText.text = _event.Name.ToString();
            if (yearText != null)
                yearText.text = $"Year: {_event.Year}";
            if (descriptionText != null)
                descriptionText.text = _event.Description.ToString();
        }

        private void OnSelect()
        {
            OnEventSelected?.Invoke(_event);
        }
    }
} 