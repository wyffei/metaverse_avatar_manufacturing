using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 导入场景管理命名空间
using TMPro; // 导入TextMeshPro命名空间

public class sceneDropDown : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown dropDown; // 使用TextMeshPro的Dropdown
    [SerializeField]
    private Button loadButton; // 按钮

    void Start()
    {

        // 设置下拉菜单选项
        dropDown.ClearOptions();
        dropDown.AddOptions(new List<string> { "skilled operator", "novice operator" });

        // 给按钮添加点击事件
        loadButton.onClick.AddListener(OnButtonClicked);
    }

    // 按钮点击事件
    void OnButtonClicked()
    {
        // 根据下拉菜单的选择加载不同的场景
        int selectedValue = dropDown.value;

        // 判断Dropdown的选项，加载对应场景
        if (selectedValue == 0)
        {
            // 选择了Option 1，加载场景1
            SceneManager.LoadScene("skilled");
        }
        else if (selectedValue == 1)
        {
            SceneManager.LoadScene("novice");
        }
        else
        {
            Debug.Log("No scene associated with this option");
        }
    }
}
