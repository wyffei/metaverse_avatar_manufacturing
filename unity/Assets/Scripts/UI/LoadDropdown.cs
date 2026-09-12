using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadDropdown : MonoBehaviour
{

    public TMP_Dropdown dropdown;  // TextMesh Pro 下拉菜单
    // Start is called before the first frame update
    void Start()
    {
        // 加载存储的下拉菜单选项
        LoadDropdownOptions();
    }

    // 加载下拉菜单选项
    void LoadDropdownOptions()
    {
        // 清空现有的选项
        dropdown.options.Clear();

        // 加载存储的选项数量
        int totalSteps = PlayerPrefs.GetInt("TotalSteps", 0);

        // 根据存储的数据加载选项
        for (int i = 1; i <= totalSteps; i++)
        {
            string optionName = PlayerPrefs.GetString("step_" + i.ToString());
            dropdown.options.Add(new TMP_Dropdown.OptionData(optionName));
        }

        // 刷新下拉菜单显示
        dropdown.RefreshShownValue();
    }
}
