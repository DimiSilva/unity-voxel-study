using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasNav : MonoBehaviour
{
    public Button[] buttons;
    int buttonIndex = 0;

    void Start()
    {
        GameObject buildBlocksPanel = GameObject.Find("Build Blocks Panel");
        buttons = buildBlocksPanel.GetComponentsInChildren<Button>();

        if (buttons != null)
            SelectButton(0);
    }

    void SelectButton(int index)
    {
        buttons[index].Select();
        buttons[index].onClick.Invoke();
        buttonIndex = index;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && buttons.Length > 1)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (buttonIndex <= 0)
                    buttonIndex = buttons.Length;
                buttonIndex--;
            }
            else
            {
                if (buttons.Length <= buttonIndex + 1)
                    buttonIndex = -1;
                buttonIndex++;
            }
            SelectButton(buttonIndex);
        }
    }
}
