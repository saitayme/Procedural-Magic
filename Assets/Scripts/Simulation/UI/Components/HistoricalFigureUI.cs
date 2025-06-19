using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.UI.Components
{
    public class HistoricalFigureUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI lifespanText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button selectButton;

        private HistoricalFigure _figure;
        public System.Action<HistoricalFigure> OnFigureSelected;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelect);
            }
        }

        public void Initialize(HistoricalFigure figure)
        {
            _figure = figure;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (nameText != null)
                nameText.text = _figure.Name.ToString();
            if (lifespanText != null)
                lifespanText.text = $"{_figure.BirthYear} - {_figure.DeathYear}";
            if (descriptionText != null)
                descriptionText.text = _figure.Description.ToString();
        }

        private void OnSelect()
        {
            OnFigureSelected?.Invoke(_figure);
        }
    }
} 