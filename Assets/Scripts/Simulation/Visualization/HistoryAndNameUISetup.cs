using UnityEngine;
using ProceduralWorld.Simulation.UI;

namespace ProceduralWorld.Simulation.Visualization
{
    public class HistoryAndNameUISetup : MonoBehaviour
    {
        public WorldHistoryUI historyUI;
        public WorldNameDisplay nameDisplay;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H) && historyUI != null)
            {
                if (historyUI.gameObject.activeSelf)
                    historyUI.CloseHistoryPanel();
                else
                    historyUI.OpenHistoryPanel();
            }
            if (Input.GetKeyDown(KeyCode.N) && nameDisplay != null)
            {
                nameDisplay.gameObject.SetActive(!nameDisplay.gameObject.activeSelf);
            }
        }
    }
} 