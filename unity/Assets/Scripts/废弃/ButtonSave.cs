using UnityEngine;
using System.IO;
using System.Threading;
using UnityEngine.UI; // 引用 UnityEngine.UI 命名空间
using System.Collections.Generic;

public class ButtonSave : MonoBehaviour
{
    public UDPReceiver udpReceiver; // 在 Inspector 中引用 UDPReceiver 脚本
    public Button saveButton;       // 在 Inspector 中引用开始按钮
    public Button stopButton;       // 在 Inspector 中引用停止按钮
    public Button readButton;       // 在 Inspector 中引用读取按钮（按钮 3）

    private Thread storeThread;
    //private bool isStoringData = false; // 控制是否开始存储数据
    private string filePath;

   /* void Start()
    {
        // 设置存档文件路径为 txt 文件
        filePath = Path.Combine(Application.dataPath, "Data", "Test.txt");

        // 给按钮添加点击监听事件
        saveButton.onClick.AddListener(OnSaveButtonClick);
        stopButton.onClick.AddListener(OnStopButtonClick);
        readButton.onClick.AddListener(OnReadButtonClick);

        Debug.Log("Data will be saved to: " + Path.GetFullPath(filePath));
    }

    void OnApplicationQuit()
    {
        isStoringData = false;
        storeThread?.Abort();
    }

    private void OnSaveButtonClick()
    {
        DeleteAndRecreateFile();

        if (!isStoringData)
        {
            isStoringData = true;
            storeThread = new Thread(new ThreadStart(StoreDataThread));
            storeThread.IsBackground = true;
            storeThread.Start();
            Debug.Log("Started saving data.");
        }
        else
        {
            Debug.Log("Data saving is already running.");
        }
    }

    private void DeleteAndRecreateFile()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath); // 删除文件
            Debug.Log("File deleted successfully.");
        }

        // 确保目录存在
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Create(filePath).Close(); // 创建新的 txt 文件
        Debug.Log("File created successfully.");
    }

    private void StoreDataThread()
    {
        while (isStoringData)
        {
            if (udpReceiver != null && !string.IsNullOrEmpty(udpReceiver.ReceivedData))
            {
                string receivedData = udpReceiver.ReceivedData;

                // 将文本数据追加到文件
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine(receivedData); // 直接写入文本数据到新的一行
                }

                Debug.Log($"Data saved to: {filePath}");
            }

            Thread.Sleep(50); // 减少磁盘写入频率，每秒20次
        }
    }

    private void OnStopButtonClick()
    {
        StopDataSaving();
    }

    public void StopDataSaving()
    {
        isStoringData = false;
        Debug.Log("Data saving stopped.");
    }

    private void OnReadButtonClick()
    {
        TestReadData();
    }

    public void TestReadData()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        List<string> messages = new List<string>();

        using (StreamReader sr = new StreamReader(filePath))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    messages.Add(line); // 直接读取每一行文本
                }
            }
        }

        // 输出所有字符串内容
        foreach (var message in messages)
        {
            Debug.Log(message);
        }
    }*/
}
