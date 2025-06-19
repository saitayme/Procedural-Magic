using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.UI.Components
{
    public class HistoricalPeriodUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI yearsText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button selectButton;

        private HistoricalPeriod _period;
        public System.Action<HistoricalPeriod> OnPeriodSelected;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelect);
            }
        }

        public void Initialize(HistoricalPeriod period)
        {
            _period = period;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (nameText != null)
                nameText.text = _period.Name.ToString();
            if (yearsText != null)
                yearsText.text = $"{_period.StartYear} - {_period.EndYear}";
            if (descriptionText != null)
                descriptionText.text = _period.Description.ToString();
        }

        private void OnSelect()
        {
            OnPeriodSelected?.Invoke(_period);
        }
    }
} 