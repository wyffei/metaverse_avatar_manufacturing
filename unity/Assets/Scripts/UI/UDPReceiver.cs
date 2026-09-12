using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 引用 UnityEngine.UI 命名空间
using BodyTracking;  // 引用包含 Landmark 和 LandmarkList 类的命名空间
using TMPro;
using System;//Console
using System.IO;
using System.Net;//IPEndPoint
using System.Net.Sockets;//UdpClient
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using Unity.VisualScripting.Antlr3.Runtime;
//using UnityEditor.Experimental.GraphView;
//using System.Linq;
//using System.Security.AccessControl;

public class UDPReceiver : MonoBehaviour
{
    private UdpClient udpClient;// 创建 UdpClient 实例
    private const int port = 9600;  // 选择适当的端口

    // 用于存储线程等待接收线程和主线程的数据
    private AutoResetEvent receiveThreadDataReceived = new AutoResetEvent(false);
    private AutoResetEvent mainThreadDataReceived = new AutoResetEvent(false);
    private Thread storethread;//存储线程
    private Thread receiveThread;//接收线程
    //private bool isstoreThreadRunning = false; //isrecording
    private bool isreceiveThreadRunning = false;

    private string mainThreadData;//主线程，小车零件的坐标

    private string receivedData;//接收线程，人体关键点坐标
    private string carpartsData; //从存储文件中拿出来的小车零件坐标
    private string pendingData; // 用于存储非主线程中修改的数据
    private Vector3[] targetPositions = new Vector3[9];  // 存储9个物体的位置
    public Rigidbody[] carPartsRigidbodies = new Rigidbody[9];    // 物体的 Rigidbody 数组（9个物体）
    private CancellationTokenSource cancellationTokenSource2;// 声明 CancellationTokenSource 和 CancellationToken
    private CancellationToken cancellationToken2;

    private string storeData;//存储线程，写入文件的数据
    private CancellationTokenSource cancellationTokenSource1;// 声明 CancellationTokenSource 和 CancellationToken
    private CancellationToken cancellationToken1;

    private string dataFolderPath; // Data 文件夹路径
    private string filePath;

    private bool isRecording = false; // 是否正在录制
    private bool isPlaying = false;  // 是否正在播放
    public Button saveButton;       // 在 Inspector 中引用开始按钮
    public Button stopButton;       // 在 Inspector 中引用停止按钮
    public Button playButton;       // 在 Inspector 中引用播放按钮
    public TMP_Dropdown dropDown; // 下拉菜单
    public GameObject panel;  // 面板


    void Start()
    {
        if (dropDown != null)
        {
            // 定义 Data 文件夹路径
            dataFolderPath = Path.Combine(Application.dataPath, "Data");
            // 确保 Data 文件夹存在，不存在则创建
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
                Debug.Log($"创建文件夹: {dataFolderPath}");
            }
            // 监听下拉菜单的值变化
            dropDown.onValueChanged.AddListener(delegate { UpdateFilePath(); });
        }
        // 检查按钮是否为空，然后为每个按钮添加点击事件监听
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButtonClick);//主线程
        }
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopButtonClick);//主线程
        }
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClick);//主线程
        }
        if (dropDown.options.Count > 0)
        {
            // skilled场景：如果下拉菜单有选项，手动触发一次 UpdateFilePath
            UpdateFilePath();
        }
        if (!isreceiveThreadRunning)
        {
            StartReceiveThread();
            if (udpClient == null)
            {
                udpClient = new UdpClient(port);  // 确保 udpClient 已初始化
                Debug.Log("UdpClient已开启");
            }
        }

        carpartsData = InitializeCarPartsData(); //初始化小车零件坐标

    }

// 初始化字符串，每个数据为0
private string InitializeCarPartsData()
{
    // 创建一个包含 27 个 0 的数组
    string[] data = new string[27];
    for (int i = 0; i < 27; i++)
    {
        data[i] = "0";
    }

    // 将数组转换为以空格分隔的字符串
    return string.Join(" ", data);
}
private void Update()
    {
        mainThreadData = SavePositions();
        mainThreadDataReceived.Set();
        // 在主线程中检查是否有待处理的数据
        //默认情况下，通过属性的 Setter 修改 CarPartsData 以及调用的 ParseAndMoveCarParts() 方法都在主线程中执行。
        //如果需要在非主线程中修改数据，需要使用线程安全的机制（如队列或回调）将数据传递回主线程。
        if (pendingData != null)
        {
            CarPartsData = pendingData; // 在主线程中更新数据
            pendingData = null; // 清空待处理数据
        }
    }

    // Getter 和 Setter，用于监控 carpartsData 的变化
    public string CarPartsData 
    {
        get { return carpartsData; }
        set
        {
            if (carpartsData != value) // 检查数据是否发生变化
            {
                carpartsData = value;
                Debug.Log("移动小车零件");
                ParseAndMoveCarParts();//当外部代码修改 CarPartsData 时，Setter 会触发 ParseAndMoveCarParts() 方法。
            }
        }
    }


    // 解析 carpartsData 并将物体移动到新的位置
    private void ParseAndMoveCarParts()
    {
        // 假设 carpartsData 是以空格分隔的字符串 "x1 y1 z1 x2 y2 z2 ... x9 y9 z9"
        string[] positionValues = carpartsData.Split(' ');
        if (positionValues.Length == 27) // 9个物体，每个物体有3个坐标值
        {
            for (int i = 0; i < 9; i++)
            {
                float x = float.Parse(positionValues[i * 3]);
                float y = float.Parse(positionValues[i * 3 + 1]);
                float z = float.Parse(positionValues[i * 3 + 2]);
                targetPositions[i] = new Vector3(x, y, z);

                // 移动物体到新的位置
                if (carPartsRigidbodies[i] != null)
                {
                    carPartsRigidbodies[i].MovePosition(targetPositions[i]);
                    Debug.Log($"物体 {i} 已移动到: {targetPositions[i]}");
                }
                else
                {
                    Debug.LogError($"Rigidbody {i} 未分配！");
                }

            }
        }
        else
        {
            Debug.LogError("carpartsData 的格式不正确！应该包含27个数值（9个物体，每个物体3个坐标分量）。");
        }
    }

    // 调用此方法保存指定物体的位置
    public string SavePositions()
    {
        // 使用 StringBuilder 来构建字符串
        StringBuilder positionsString = new StringBuilder();

        // 遍历 Rigidbody 数组，保存它们的位置
        foreach (Rigidbody rb in carPartsRigidbodies)
        {
            if (rb != null)
            {
                Vector3 pos = rb.transform.position;
                // 只添加坐标数据，不包含名称，不换行
                positionsString.Append($"{pos.x} {pos.y} {pos.z} ");
            }
        }

        return positionsString.ToString().Trim();  // 返回拼接后的字符串，去掉最后的空格
    }

    // 启动接收线程
    void StartReceiveThread()
    {
        // 创建 CancellationTokenSource 和 CancellationToken
        cancellationTokenSource2 = new CancellationTokenSource();
        cancellationToken2 = cancellationTokenSource2.Token;

        receiveThread = new Thread(() => ReceiveData(cancellationToken2));  // 创建线程
        receiveThread.IsBackground = true;  // 设置为后台线程
        receiveThread.Start();
        isreceiveThreadRunning = true;
        Debug.Log("2接收线程已启动");

    }

    // 停止接收线程
    void StopReceiveThread()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            cancellationTokenSource2.Cancel();  // 请求取消线程
            receiveThread.Join();  // 等待线程安全结束
            receiveThread = null;
            isreceiveThreadRunning = false;
            Debug.Log("2接收线程已停止");
        }
    }
    public void ReleaseResources()
    {
        StopReceiveThread();
        StopThread1();
        if (udpClient != null)
        {
            udpClient.Close();  // 关闭 UdpClient
            
        }
    }
    // 在销毁时停止线程. 当场景切换时，当前场景中的所有对象（除非使用了 DontDestroyOnLoad）会被销毁，OnDestroy 会在这些对象被销毁时调用。
    void OnDestroy()
    {
        Debug.Log("UdpClient 已关闭");
        ReleaseResources();
    }

    // filePath
    private void UpdateFilePath()
    {
        // 根据下拉菜单当前选项值更新文件路径
        string dropdownValue = dropDown.options[dropDown.value].text;
        filePath = Path.Combine(dataFolderPath, $"{dropdownValue}.txt");
        Debug.Log($"文件路径已更新为: {filePath}");
    }


    // 接收数据线程
    private void ReceiveData(CancellationToken token)// receivethreadWork
    {
        while (!token.IsCancellationRequested)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            int currentLineIndex = 0;  // 用于跟踪当前播放的行
            while (true)
            {
                // 在这里检查取消请求，避免在 Receive 时卡住
                if (token.IsCancellationRequested) break;

                // 如果是播放模式，读取文件并播放一行数据
                if (isPlaying)
                {
                    UpdateFilePath();//novice场景
                    if (File.Exists(filePath))
                    {
                        // 播放，读取文件数据进行播放
                        Debug.Log($"播放文件路径为: {filePath}");

                        string[] lines = File.ReadAllLines(filePath);

                        // 判断当前行是否小于文件行数
                        if (currentLineIndex < lines.Length)
                        {
                            //加入延时，使其正常速度播放，60FPS，1/60秒 ≈ 16.67毫秒，但快，所以加大
                            Thread.Sleep(60);
                            string lineToPlay = lines[currentLineIndex];
                            // 这里处理每一行数据的播放逻辑
                            if (lineToPlay.StartsWith("1:")) receivedData = lineToPlay.Substring(2);  // 英文字符。从索引2开始，去掉前两个字符
                            if (currentLineIndex==1 && lineToPlay.StartsWith("2:")) pendingData = lineToPlay.Substring(2); //第0行是人体的，下一行第1行才是小车的, 将数据存储在 pendingData 中
                            //Debug.LogWarning("1："+ receivedData);
                            //Debug.LogWarning("2：" + carpartsData);
                            // 增加行索引，准备播放下一行
                            currentLineIndex++;
                        }
                        else
                        {
                            currentLineIndex = 0;
                            isPlaying = false;
                            Debug.Log("Play complete.");
                        }
                    }
                    else
                    {
                        Debug.LogError("File not found for playback.");
                    }
                }
                else
                {
                    // 如果是录制模式，则将接收到的数据存储到文件中
                    if (isRecording)
                    {
                        // 接收数据并传递给 byte[] 数组
                        byte[] data = udpClient.Receive(ref endPoint); // 接收数据                    
                        receivedData = Encoding.UTF8.GetString(data);
                        receiveThreadDataReceived.Set(); // 通知线程1数据已准备好
                    }
                }

            }
            Debug.Log("2接收线程正常结束");
        }
    }

    // 按钮点击事件
    private void OnSaveButtonClick()
    {

        panel.SetActive(false);//关闭面板
        if (!isRecording)
        {
            isRecording = true;
            if (!File.Exists(filePath)) 
        {
            // 如果文件不存在，创建文件
            File.Create(filePath).Close(); // 创建文件并立即关闭
        }
            else {  File.WriteAllText(filePath, string.Empty);}

            Debug.Log("Started recording data.");
            StartThread1();
        }
        else { File.WriteAllText(filePath, string.Empty); }
    }

    // 启动线程1
    private void StartThread1()
    {
        // 创建 CancellationTokenSource 和 CancellationToken
        cancellationTokenSource1 = new CancellationTokenSource();
        cancellationToken1 = cancellationTokenSource1.Token;

        storethread = new Thread(() => storeThreadWork(cancellationToken1));  // 创建线程
        storethread.IsBackground = true;  // 设置为后台线程
        storethread.Start();
        Debug.Log("1存储线程已启动");
    }
    // 停止线程1
    private void StopThread1()
    {
        if (storethread != null && storethread.IsAlive)
        {
            cancellationTokenSource1.Cancel();  // 请求取消线程
            storethread.Join();  // 等待线程安全结束
            storethread = null;
            Debug.Log("1存储线程已停止");
        }
    }
    private void storeThreadWork(CancellationToken token) // receivethreadWork
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 检查是否请求取消
                if (token.IsCancellationRequested) break;

                // 等待人体关键点数据
                receiveThreadDataReceived.WaitOne();
                // 检查取消请求
                if (token.IsCancellationRequested) break;

                // 写入人体数据
                try
                {
                    using (StreamWriter sw = new StreamWriter(filePath, append: true))  // append: true 表示追加模式
                    {
                       // Debug.Log("1");
                        sw.WriteLine("1:" + receivedData);  // 将数据写入文件
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("写入人体数据失败: " + ex.Message);
                }

                // 等待小车坐标数据
                mainThreadDataReceived.WaitOne();
                // 检查取消请求
                if (token.IsCancellationRequested) break;

                // 写入小车数据
                try
                {
                    using (StreamWriter sw = new StreamWriter(filePath, append: true))
                    {
                        //Debug.Log("2");
                        sw.WriteLine("2:" + mainThreadData); 
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("写入小车数据失败: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("存储线程异常: " + ex.Message);
        }
        finally
        {
            // 线程退出时的清理工作
            Debug.Log("1存储线程已正常结束");
        }
    }


    // 按钮点击事件，停止录制数据
    private void OnStopButtonClick()
    {
        if (isRecording)
        {
            isRecording = false;
            StopThread1();
            Debug.Log("Stopped recording data.");
        }
    }

    // 按钮点击事件，开始播放数据
    private void OnPlayButtonClick()
    {
        isPlaying = true;
        Debug.Log("Started playing data.");
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