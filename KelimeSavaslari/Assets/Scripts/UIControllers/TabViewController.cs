using UnityEngine;
using UnityEngine.UI;

public class TabViewController : MonoBehaviour
{
    public GameObject[] tabPanels;
    public Button[] tabButtons;

    void Start()
    {
        ShowTab(0);
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i; // closure fix
            tabButtons[i].onClick.AddListener(() => ShowTab(index));
        }
    }
    public void ShowTab(int index)
    {
        for (int i = 0; i < tabPanels.Length; i++)
        {
            tabPanels[i].SetActive(i == index);
        }
    }
}
