using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.Scripting.Python;
using TMPro;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;  // 引入Task

public class AQAtask : MonoBehaviour
{
    public Button addButton;//AQA按钮
    public TMP_Dropdown dropDown;
    public TextMeshProUGUI assesstext;
    public GameObject panel;  // 面板

    string dropdownValue;
    int exitCode=-1;
    float score;

    void Start()
    {
        addButton.onClick.AddListener(OnAddButtonClick);
        dropdownValue = "step_1";
        dropDown.onValueChanged.AddListener(delegate { UpdateFilePath(); });

        // 确保文字在一行显示
        assesstext.enableWordWrapping = false;  // 禁用自动换行
        assesstext.overflowMode = TextOverflowModes.Overflow;  // 允许超出容器
        assesstext.alignment = TextAlignmentOptions.Center; // 文字居中
        
        panel.SetActive(false); // 初始化时隐藏面板
    }

    void OnAddButtonClick()
    {
        // 修改字号和颜色
        assesstext.fontSize = 28f; // 设置字体大小
        Color newColor1; // 设置字体颜色FF6838
        if (ColorUtility.TryParseHtmlString("#4CAAB5", out newColor1))
        {
            assesstext.color = newColor1;
        }
        
        assesstext.text = "Under evaluation";
        panel.SetActive(true);
        RunPythonScriptAsync(dropdownValue);

    }

    async void RunPythonScriptAsync(string dropdownValue)
    {
        await Task.Run(() => RunPythonScript(dropdownValue));
    }
    private void Update()
    {
        if (exitCode == 0)
        {
            exitCode = -1;
            // 修改字号和颜色
            assesstext.fontSize = 24f; // 设置字体大小
            if (score < 0.9f)
                {
                Color newColor2; // 设置字体颜色FF6838
                if (ColorUtility.TryParseHtmlString("#FF6838", out newColor2))
                {
                    assesstext.color = newColor2;
                }

                assesstext.text = $"The evaluation score is {score}, it is recommended to re-record.";
                }
                else
            { 
                Color newColor3; // 设置字体颜色
            if (ColorUtility.TryParseHtmlString("#4FF14D", out newColor3))
            {
                assesstext.color = newColor3;
            }
                assesstext.text = $"The evaluation score is {score}, the result is good.";
                }

        }
    }

    void RunPythonScript(string dropdownValue)
    {
        Process process = new Process();
        process.StartInfo.FileName = @"D:\anaconda\envs\venv\python.exe";
        process.StartInfo.Arguments = $@"E:/Unity/wyf/try/Assets/Python_AQA/Neural_Networks/ownbody.py {dropdownValue}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.OutputDataReceived += new DataReceivedEventHandler(GetData);

        process.Start();
        process.BeginOutputReadLine();

        process.WaitForExit();  // 这里不会阻塞主线程，因为它在 Task 里运行
        exitCode = process.ExitCode;
        UnityEngine.Debug.Log($"Python Exit Code: {exitCode}");

        process.Close();
    }

    void GetData(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            if (float.TryParse(e.Data, out score))
            {
                UnityEngine.Debug.Log("Python output: " + score);
                score = (float)((int)(score * 100) / 100.0); 
            }
            else
            {
                UnityEngine.Debug.LogError($"failed，e.Data = {e.Data}");
            }
        }
    }

    private void UpdateFilePath()
    {
        dropdownValue = dropDown.options[dropDown.value].text;
    }
}
