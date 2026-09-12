using BodyTracking;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.PlayerSettings;

public class BodySolver : MonoBehaviour
{
    [SerializeField] private UDPReceiver udpReceiver;  // 通过 inspector 链接到 UdpReceiver
    private WalkAnimation walkAnimation;
    Animator animator;

    private Vector3 hipsPosition;
    public float epsilonforstable = 0.5f; // 设定容差值
    private int stableFrameCount = 0; // 连续稳定帧数
    public int maxStableFrames = 15;  // 设置为多少帧判断为稳定不变
    public float overallmultiple = 5f;
    public float zmultiple = 5f;
    // 用于保存上次的hipsPosition
    private Vector3 previousHipsPosition;

    // 计算出的位移
    [SerializeField] public Vector3 hipsNowPosition;
    // 为了在 Inspector 中显示 hipsshift 的值
    [SerializeField] public bool hipsshift;  // 使用 SerializeField 或 public 显示在 Inspector 中
    [SerializeField] public Quaternion overallRotation = Quaternion.identity;

    const float SMOOTHING = 0.1f;   // lower value = smoother, but less responsive

    // landmark indices
    const int HIPS = 0;
    const int SPINE = 7;
    const int CHEST = 8;
    const int UPPERCHEST = 9;
    const int NECK = 10; //父骨骼是spine02，即UPPERCHEST

    const int LEFTSHOULDER = 12;//同14，15，但不同组件，用于控制上身旋转。尝试？
    const int RIGHTSHOULDER = 13;
    const int LEFTUPPERLEG = 1;
    const int RIGHTUPPERLEG = 2;

    // 使用 [SerializeField] 来暴露给 Inspector，便于在编辑器中拖拽赋值

    [SerializeField] private Transform hips;
    [SerializeField] private bool mirrorMode;

    [SerializeField] private Transform spine;
    [SerializeField] private Transform spine1;
    [SerializeField] private Transform spine2;
    [SerializeField] private Transform neck;

    [SerializeField] private Transform lShoulderTf;
    [SerializeField] private Transform rShoulderTf;
    [SerializeField] private Transform lUlegTf;
    [SerializeField] private Transform rUlegTf;

    private (Transform, Transform)[] transformPairs;
    private (int, int)[] landmarkIdxPairs;

    private LandmarkList poseLandmarks;
    private Quaternion[] prevRots;

    // 用于存储运行 5 秒后收集的前 10 次数据
    private Queue<Vector3> recentHipsPositions = new Queue<Vector3>();
    private Vector3 initialHipsPosition = Vector3.zero;  // 原始位置

    private float startTime; // 程序启动时间
    [SerializeField] public bool isCollectingData = false; // 标志位，指示是否开始收集数据
    private StandBench standBench;

    void Start()
    {

        Application.targetFrameRate = 60;  // 限制帧率为 60 FPS
        // 获取当前物体上的 WalkAnimation 组件
        walkAnimation = GetComponent<WalkAnimation>();
        // 获取“动画制作者组件”
        animator = GetComponent<Animator>();
        if (mirrorMode)
        { (lShoulderTf, rShoulderTf) = (rShoulderTf, lShoulderTf); }

        transformPairs = new (Transform, Transform)[]{ (hips,spine),(spine,spine1),(spine1,spine2),(spine2,neck),//0 1 2 3 
                                                       (spine2,lShoulderTf),(spine2,rShoulderTf) };//4 5

        landmarkIdxPairs = new (int, int)[]{ (HIPS,SPINE),(SPINE,CHEST),(CHEST,UPPERCHEST),(UPPERCHEST,NECK),
                                              (UPPERCHEST,LEFTSHOULDER),(UPPERCHEST,RIGHTSHOULDER) };

        poseLandmarks = null;

        prevRots = new Quaternion[transformPairs.Length];
        for (int i = 0; i < prevRots.Length; i++)
        {
            prevRots[i] = transformPairs[i].Item1.localRotation;
        }

        startTime = Time.time;  // 记录程序开始的时间
        initialHipsPosition = hips.position;  // 记录原始位置
        initialHipsPosition.y = 0;

        animator.enabled = true;
        // 更改 isWalking 参数的值
        animator.SetBool("isWalking", false); // 设置为 false，表示停止走路
        standBench = GetComponent<StandBench>();

       
    }

    void Update()
    {
        float currentFrameRate = 1.0f / Time.deltaTime;  // Time.deltaTime 是上一帧的时间，1 / Time.deltaTime 就是当前帧率
        //Debug.Log("Current FPS: " + currentFrameRate);
        // 获取通过 UDP 接收到的数据
        string receivedData = udpReceiver.GetReceivedData();
        if (!string.IsNullOrEmpty(receivedData))
        {
            // 解析数据并设置 poseLandmarks
            LandmarkList landmarks = udpReceiver.ParseReceivedData(receivedData);
            if (landmarks != null)
            {
                SetPoseLandmarks(landmarks);
            }
        }
        for (int i = 0; i < transformPairs.Length; i++)
        {
            (Transform parentTf, Transform childTf) = transformPairs[i];
            parentTf.localRotation = Quaternion.identity;
        }

        if (poseLandmarks != null) SolvePose();
    }

    // called from HolisticTrackingSolution.cs
    public void SetPoseLandmarks(LandmarkList poseWorldLandmarks)
    {
        poseLandmarks = poseWorldLandmarks;
        //Debug.Log("Received landmarks count: " + poseLandmarks.landmarks.Length);
    }

    private void SolvePose()
    {
        if (poseLandmarks == null || poseLandmarks.landmarks == null || poseLandmarks.landmarks.Length == 0)
        {
            Debug.LogError("Pose landmarks are not initialized or empty.");
            return;
        }
        // convert Landmarks to Vector3s
        Vector3[] landmarks = new Vector3[poseLandmarks.landmarks.Length];

        int[] specificIndices = { 0, 7, 8, 9, 10, 12, 13, 66 };
        foreach (int i in specificIndices)
        {
            landmarks[i] = new Vector3(poseLandmarks.landmarks[i].x, poseLandmarks.landmarks[i].y, poseLandmarks.landmarks[i].z);  // 访问 landmarks 数组
        }
        if (isCollectingData)
        {
            // 在循环外部定义四元数变量
            Quaternion shoulderRotation = Quaternion.identity;
            Quaternion spine2Rotation = Quaternion.identity;
            Quaternion spine1Rotation = Quaternion.identity;
            Quaternion spineRotation = Quaternion.identity;

            for (int i = 0; i < transformPairs.Length; i++)
            {
                (Transform parentTf, Transform childTf) = transformPairs[i];
                (int parentLmIdx, int childLmIdx) = landmarkIdxPairs[i];

                // current avatar limb direction, in joint Transform's local space
                Vector3 vOld = (childTf.position - parentTf.position).normalized;
                vOld = parentTf.InverseTransformDirection(vOld);

                // current player limb direction, in joint Transform's local space
                Vector3 vNew = (landmarks[childLmIdx] - landmarks[parentLmIdx]).normalized;
                vNew = parentTf.InverseTransformDirection(vNew);

                // smooth, interpolated rotation           
                Quaternion rotOld = prevRots[i];
                Quaternion rotNew = parentTf.localRotation * Quaternion.FromToRotation(vOld, vNew).normalized;

                // 这里不需要每次重新声明四元数，直接赋值
                if (i == 4) { shoulderRotation = rotNew; }
                if (i == 2) { spine2Rotation = rotNew; }
                if (i == 1) { spine1Rotation = rotNew; }
                if (i == 0)
                {
                    spineRotation = rotNew;

                }
                if (i == 3) { neck.rotation = Quaternion.identity; continue; }

                if (i != 12 && i != 13 && i != 0) { parentTf.localRotation = Quaternion.Slerp(rotOld, rotNew, SMOOTHING); }
                prevRots[i] = parentTf.localRotation;
                if (i == 0)
                {
                    // 按顺序乘法计算从shoulder到hips的整体旋转
                    overallRotation = shoulderRotation * spine2Rotation * spine1Rotation * spineRotation;
                    // 将计算出的整体旋转应用到根骨骼（hips）
                    hips.rotation = Quaternion.Slerp(rotOld, overallRotation, SMOOTHING);
                }
            }
        }
        // 将 hips 位移更新为 landmarks[66]
        hipsPosition = landmarks[66];
        //Debug.Log("Mediapipe Hips Position: " + landmarks[66]);
        hipsPosition.y = 0;
        hipsPosition.z *= zmultiple;

        // 程序运行 5 秒后开始收集数据
        if (Time.time - startTime > 5f) // 如果程序运行超过 5 秒
        {
            isCollectingData = true;
           
                                                 
        }

        // 收集前 10 次数据
        if (isCollectingData)
        {
            // 添加当前的 hipsPosition 到队列
            if (recentHipsPositions.Count < 10)
            {
                recentHipsPositions.Enqueue(hipsPosition);
            }
            else
            {
                Vector3 totalPosition = Vector3.zero;
                foreach (var pos in recentHipsPositions)
                {
                    totalPosition += pos;
                }
                Vector3 averagePosition = totalPosition / recentHipsPositions.Count;

                // 计算当前 hipsPosition 与初始位置（或平均位置）之间的偏移
                Vector3 offset = hipsPosition - averagePosition;
                offset *= overallmultiple;

                hipsNowPosition = initialHipsPosition + offset;
                animator.SetBool("isWalking", true);  // 设置为 true，表示正在走路
                // 根据偏移调整模型位置
                //hips.position = hipsDifferPosition;
            }



        // 设定容差值
        float epsilon = 1e-6f;

        // 判断 landmarks[66] 是否近似为 0，并修改 hipsshift 变量
        if (walkAnimation != null && !standBench.isInRange)
        {
            // 根据 hipsPosition 是否为零，来修改 hipsshift 值
            if (Mathf.Abs(hipsPosition.x) < epsilon && Mathf.Abs(hipsPosition.y) < epsilon && Mathf.Abs(hipsPosition.z) < epsilon)
            {
                stableFrameCount = 0;  // 位置为零，重置稳定帧数
                hipsshift = false;
            }
            else
            {
                // 如果连续几帧的位置几乎没有变化，则设置为 false
                if (Mathf.Abs(hipsPosition.x - previousHipsPosition.x) < epsilonforstable &&
                    Mathf.Abs(hipsPosition.y - previousHipsPosition.y) < epsilonforstable &&
                    Mathf.Abs(hipsPosition.z - previousHipsPosition.z) < epsilonforstable)
                {
                    stableFrameCount++;
                }
                else
                {
                    stableFrameCount = 0;  // 如果位置发生变化，则重置稳定帧数
                }

                if (stableFrameCount >= maxStableFrames)
                {
                    hipsshift = false;  // 如果连续几帧几乎不变，则认为停止
                }
                else
                {
                    hipsshift = true;
                }
            }

            // 保存当前的hipsPosition，供下次帧使用
            previousHipsPosition = hipsPosition;


            animator.enabled = hipsshift;

        }
        }
    }
}
