using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using UnityEngine.UI; // 引入UI相关的命名空间
using UnityEditor.Scripting.Python;
using TMPro;
using System.Text;

public class AQA : MonoBehaviour
{
    public Button addButton;       // 按钮，用于添加选项
    public TMP_Dropdown dropDown; // 下拉菜单
    public GameObject panel;  // 面板
    public TextMeshProUGUI text1;        // 评估中/评估结束
    public TextMeshProUGUI text2;        // 分数及建议

    string dropdownValue;

    // Start is called before the first frame update
    void Start()
    {
        addButton.onClick.AddListener(OnAddButtonClick);
        dropdownValue = "step_1";
        dropDown.onValueChanged.AddListener(delegate { UpdateFilePath(); });
        panel.SetActive(false); // 初始化时隐藏面板

    }

    // 按钮点击事件：给下拉菜单添加选项
    void OnAddButtonClick()
    {
        RunPythonScript(dropdownValue);  // 调用运行Python脚本的方法并传入参数
                                         // 弹出面板
        panel.SetActive(true);
    }

    void RunPythonScript(string dropdownValue)
    {
        //UnityEngine.Debug.Log("开始运行 Python 脚本...");

        // 设置文字内容
        text1.text = "Under evaluation";
        Process process = new Process();
        process.StartInfo.FileName = @"D:\anaconda\envs\venv\python.exe"; // 指定 Python 解释器路径
        UnityEngine.Debug.Log($"AQA文件为:" + dropdownValue);
        process.StartInfo.Arguments = $@"E:/Unity/wyf/try/Assets/Python_AQA/Neural_Networks/ownbody.py {dropdownValue}";  // Python 脚本路径 + 参数
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        // 设置正确的编码为 UTF-8
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

        // 订阅输出数据
        process.OutputDataReceived += new DataReceivedEventHandler(GetData);

        // 启动进程
        process.Start();
        process.BeginOutputReadLine();  // 开始异步读取输出
        
        /*// 读取 Python 进程的标准输出
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();*/

        process.WaitForExit(); // 阻塞，直到 Python 进程结束

       int exitCode = process.ExitCode; // 读取 Python 的退出代码
        UnityEngine.Debug.Log($"Python Exit Code: {exitCode}");
       /*  UnityEngine.Debug.Log($"Python Output: {output}");
        UnityEngine.Debug.Log($"Python Error: {error}");*/

        process.Close();
        // 关闭面板
        panel.SetActive(false);
        text1.text = "Evaluation completed";
        // 进程结束后执行其他操作
        //UnityEngine.Debug.Log("Python 脚本已执行完毕！");
    }

    void GetData(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.Log("AQA score: " + e.Data);
            // 设置文字内容，格式化显示分数
            float score;
            if (float.TryParse(e.Data, out score))
            {
                if (score < 0.98f)
                {
                    text2.text = $"The evaluation score is {e.Data}, it is recommended to re-record.";
                }
                else
                {
                    text2.text = $"The evaluation score is {e.Data}, the result is good.";
                }
            }
            else
            {
                text2.text = "评分数据无效";
            }
        }
    }

    private void UpdateFilePath()
    {
        // 根据下拉菜单当前选项值更新文件路径
        dropdownValue = dropDown.options[dropDown.value].text;

    }
}