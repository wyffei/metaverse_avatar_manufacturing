using UnityEngine;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.UI;
using System.Collections.Generic;
using BodyTracking;

public class UDPskilled : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private const int port = 9600;

    private string receivedData;
    private bool isRecording = false;
    //private bool isPlaying = false;
    private string currentFilePath;

    public Button saveButton;
    public Button stopButton;
    public Button playButton;
    public Dropdown fileDropdown;  // 下拉列表组件

    private string dataDirectory;

    void Start()
    {
        // 设置跨平台存储目录
        dataDirectory = Application.persistentDataPath;
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        // 给按钮添加点击事件
        saveButton.onClick.AddListener(OnSaveButtonClick);
        stopButton.onClick.AddListener(OnStopButtonClick);
        playButton.onClick.AddListener(OnPlayButtonClick);

        // 初始化下拉列表
        fileDropdown.onValueChanged.AddListener(OnFileSelected);
        UpdateFileDropdown();

        udpClient = new UdpClient(port);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
        receiveThread.Abort();
    }

    // 接收数据线程
    private void ReceiveData()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            byte[] data = udpClient.Receive(ref endPoint);
            receivedData = Encoding.UTF8.GetString(data);

            if (isRecording)
            {
                StoreDataToFile(receivedData);
            }
        }
    }

    // 开始录制按钮点击事件
    private void OnSaveButtonClick()
    {
        if (!isRecording)
        {
            isRecording = true;
            string fileName = GetNextFileName();
            currentFilePath = Path.Combine(dataDirectory, fileName);

            Debug.Log($"Started recording to {fileName}.");
        }
    }

    // 停止录制按钮点击事件
    private void OnStopButtonClick()
    {
        if (isRecording)
        {
            isRecording = false;
            Debug.Log("Stopped recording.");

            // 更新下拉列表
            UpdateFileDropdown();
        }
    }

    // 播放按钮点击事件
    private void OnPlayButtonClick()
    {
        if (fileDropdown.options.Count == 0)
        {
            Debug.LogWarning("No files available for playback.");
            return;
        }

        string selectedFile = fileDropdown.options[fileDropdown.value].text;
        string filePath = Path.Combine(dataDirectory, selectedFile);

        if (File.Exists(filePath))
        {
            Debug.Log($"Started playing data from {selectedFile}.");
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                Debug.Log($"Playing Data: {line}");
                // 添加播放逻辑
            }
        }
        else
        {
            Debug.LogError("Selected file does not exist.");
        }
    }

    // 存储数据到文件
    private void StoreDataToFile(string data)
    {
        using (StreamWriter sw = new StreamWriter(currentFilePath, true))
        {
            sw.WriteLine(data);
        }
    }

    // 获取下一个文件名
    private string GetNextFileName()
    {
        string[] files = Directory.GetFiles(dataDirectory, "Step_*.txt");
        int maxStep = 0;

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.StartsWith("Step_"))
            {
                string stepNumber = fileName.Substring(5); // 获取数字部分
                if (int.TryParse(stepNumber, out int step))
                {
                    maxStep = Mathf.Max(maxStep, step);
                }
            }
        }

        return $"Step_{maxStep + 1}.txt";
    }

    // 更新下拉列表内容
    private void UpdateFileDropdown()
    {
        fileDropdown.ClearOptions();

        if (Directory.Exists(dataDirectory))
        {
            string[] files = Directory.GetFiles(dataDirectory, "*.txt");
            List<string> fileNames = new List<string>();

            foreach (string file in files)
            {
                fileNames.Add(Path.GetFileName(file));
            }

            fileDropdown.AddOptions(fileNames);
        }
    }

    // 下拉列表选择事件
    private void OnFileSelected(int index)
    {
        Debug.Log($"Selected file: {fileDropdown.options[index].text}");
    }

public string GetReceivedData()
{
    return receivedData;
}

// 解析接收到的数据并转换为 LandmarkList
public LandmarkList ParseReceivedData(string data)
{
    // 分割字符串并转换为浮动值
    string[] parts = data.Split(' ');

    if (parts.Length % 3 != 0)  // 至少3
    {
        Debug.LogError("Received data is malformed.");
        return null;
    }

    // 69个点
    Landmark[] landmarks = new Landmark[69];
    for (int i = 0; i < 69; i++)
    {
        float x = float.Parse(parts[i * 3]);
        float y = float.Parse(parts[i * 3 + 1]);
        float z = float.Parse(parts[i * 3 + 2]);

        landmarks[i] = new Landmark(x, y, z);
    }

    return new LandmarkList(landmarks);
}
// 解析接收到的数据并转换为LandmarkList
public LandmarkList ParseReceivedlefthandData(string data)
{
    // 分割字符串并转换为浮动值
    string[] parts = data.Split(' ');

    if (parts.Length % 3 != 0)
    {
        Debug.LogError("Received data is malformed.");
        return null;
    }

    // 只解析 self.unity_landmark[22-39,55] 范围内的数据
    Landmark[] landmarks = new Landmark[23];
    for (int i = 24; i <= 44; i++)
    {
        int index = i - 24;
        float x = float.Parse(parts[i * 3]);        // x 坐标
        float y = float.Parse(parts[i * 3 + 1]);    // y 坐标
        float z = float.Parse(parts[i * 3 + 2]);    // z 坐标

        landmarks[index] = new Landmark(x, y, z);
    }
    // 将 self.unity_landmark[22] 映射到 landmarks[21]
    float x22 = float.Parse(parts[22 * 3]);        // x 坐标
    float y22 = float.Parse(parts[22 * 3 + 1]);    // y 坐标
    float z22 = float.Parse(parts[22 * 3 + 2]);    // z 坐标
    landmarks[21] = new Landmark(x22, y22, z22);

    // 将 self.unity_landmark[67] 映射到 landmarks[22]
    float x67 = float.Parse(parts[67 * 3]);        // x 坐标
    float y67 = float.Parse(parts[67 * 3 + 1]);    // y 坐标
    float z67 = float.Parse(parts[67 * 3 + 2]);    // z 坐标
    landmarks[22] = new Landmark(x67, y67, z67);
    return new LandmarkList(landmarks);
}
public LandmarkList ParseReceivedrighthandData(string data)
{
    // 分割字符串并转换为浮动值
    string[] parts = data.Split(' ');

    if (parts.Length % 3 != 0)
    {
        Debug.LogError("Received data is malformed.");
        return null;
    }

    // 只解析 self.unity_landmark[23,40-56] 范围内的数据
    Landmark[] landmarks = new Landmark[23];
    for (int i = 45; i <= 65; i++)
    {
        int index = i - 45;  // 将从 40 到 56 的索引转换为 0 到 20 的索引
        float x = float.Parse(parts[i * 3]);        // x 坐标
        float y = float.Parse(parts[i * 3 + 1]);    // y 坐标
        float z = float.Parse(parts[i * 3 + 2]);    // z 坐标

        landmarks[index] = new Landmark(x, y, z);
    }
    // 将 self.unity_landmark[23] 映射到 landmarks[21]
    float x23 = float.Parse(parts[23 * 3]);        // x 坐标
    float y23 = float.Parse(parts[23 * 3 + 1]);    // y 坐标
    float z23 = float.Parse(parts[23 * 3 + 2]);    // z 坐标
    landmarks[21] = new Landmark(x23, y23, z23);
    // 将 self.unity_landmark[68] 映射到 landmarks[22]
    float x68 = float.Parse(parts[68 * 3]);        // x 坐标
    float y68 = float.Parse(parts[68 * 3 + 1]);    // y 坐标
    float z68 = float.Parse(parts[68 * 3 + 2]);    // z 坐标
    landmarks[22] = new Landmark(x68, y68, z68);
    //Debug.Log("Right Hand Landmarks: " + string.Join(", ", landmarks.Select(l => $"({l.x}, {l.y}, {l.z})")));
    return new LandmarkList(landmarks);
}

}