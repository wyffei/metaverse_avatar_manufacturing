using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI; // 引用 UnityEngine.UI 命名空间

public class RecordFinishButton : MonoBehaviour
{

    [SerializeField]
    private Button saveButton;       // 在 Inspector 中引用开始按钮
    [SerializeField]
    private Button stopButton;       // 在 Inspector 中引用停止按钮
    private string filePath;
    private bool isRecording = false; // 是否正在录制

    // Start is called before the first frame update
    void Start()
    {
        // 设置存档文件路径为 txt 文件
        filePath = Path.Combine(Application.dataPath, "Data", "Test.txt");

        // 给按钮添加点击监听事件
        saveButton.onClick.AddListener(OnSaveButtonClick);
        stopButton.onClick.AddListener(OnStopButtonClick);
    }

    // 按钮点击事件，开始录制数据
    private void OnSaveButtonClick()
    {
        if (!isRecording)
        {
            isRecording = true;
            Debug.Log("Started recording data.");
            // 确保数据文件存在
            if (File.Exists(filePath))
            {
                File.Delete(filePath); // 删除旧文件
            }
            // 创建新的文件
            File.Create(filePath).Close();
        }
    }

    // 按钮点击事件，停止录制数据
    private void OnStopButtonClick()
    {
        if (isRecording)
        {
            isRecording = false;
            Debug.Log("Stopped recording data.");
        }
    }

}
