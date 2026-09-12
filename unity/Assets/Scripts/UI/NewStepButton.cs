using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 引入UI相关的命名空间
using TMPro; // 引入TextMesh Pro相关的命名空间

public class NewStepButton : MonoBehaviour
{
    public Button addButton;       // 按钮，用于添加选项
    public TMP_Dropdown dropdown;      // 下拉菜单，用于显示选项
    private int stepCount = 2 ;     // 用于记录当前添加的步骤数字

    // Start is called before the first frame update
    void Start()
    {
        addButton.onClick.AddListener(OnAddButtonClick);

        // 初始化时，给下拉菜单添加默认选项 step_1
        dropdown.options.Add(new TMP_Dropdown.OptionData("step_1"));

        // 刷新Dropdown显示，确保UI更新
        dropdown.RefreshShownValue();
    }

    // 按钮点击事件：给下拉菜单添加选项
    void OnAddButtonClick()
    {
        // 根据步骤数生成选项
        string optionName = "step_" + stepCount.ToString(); //step_2

        // 将新选项添加到下拉菜单
        dropdown.options.Add(new TMP_Dropdown.OptionData(optionName));

        // 更新步骤计数
        stepCount++; // 3

        // 刷新Dropdown显示
        dropdown.RefreshShownValue();

        // 保存选项数据到 PlayerPrefs
        SaveDropdownOptions();
    }

    // 保存下拉菜单选项
    void SaveDropdownOptions()
    {
        // 保存选项的数量
        PlayerPrefs.SetInt("TotalSteps", stepCount - 1); // 2

        for (int i = 1; i < stepCount; i++)
        {
            string optionName = "step_" + i.ToString();
            PlayerPrefs.SetString("step_" + i.ToString(), optionName);  // 存储每个选项
        }

        // 保存数据
        PlayerPrefs.Save();
    }

}

