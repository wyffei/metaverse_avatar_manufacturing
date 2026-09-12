using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using BodyTracking;

public class Hand : MonoBehaviour
{
    [SerializeField] private UDPReceiver udpReceiver;  // 通过 inspector 链接到 UdpReceiver
                                                       // 获取 BodySolver 的引用
    private BodySolver bodySolver;
    private LandmarkList leftHandLandmarks, rightHandLandmarks;
    private bool leftDetected, rightDetected;
    public bool leftfist, rightfist;
    public class AvatarTree
    {
        public Transform transf;
        public AvatarTree[] childs;
        public AvatarTree parent;
        public int idx;
        public Quaternion quaternion;//起始旋转
        public AvatarTree(Transform tf, int count, int idx, Quaternion quaternion, AvatarTree parent = null)
        {
            this.transf = tf;
            this.parent = parent;
            this.idx = idx;
            this.quaternion = quaternion;
            if (count > 0)
            {
                childs = new AvatarTree[count];
            }
        }
        public Vector3 GetDir()
        {
            if (parent != null)
            {
                return transf.position - parent.transf.position;
            }
            return Vector3.up;
        }
    }
    public Transform l_wrist;//左手腕
    public Transform l_thumb1;//左拇指
    public Transform l_thumb2;
    public Transform l_thumb3;
    public Transform l_index1;//左食指
    public Transform l_index2;
    public Transform l_index3;
    public Transform l_middle1;//左中指
    public Transform l_middle2;
    public Transform l_middle3;
    public Transform l_ring1;//左无名指
    public Transform l_ring2;
    public Transform l_ring3;
    public Transform l_pinky1;//左小拇指
    public Transform l_pinky2;
    public Transform l_pinky3;
    public Transform r_wrist;//右手腕
    public Transform r_thumb1;//右拇指
    public Transform r_thumb2;
    public Transform r_thumb3;
    public Transform r_index1;//右食指
    public Transform r_index2;
    public Transform r_index3;
    public Transform r_middle1;//右中指
    public Transform r_middle2;
    public Transform r_middle3;
    public Transform r_ring1;//右无名指
    public Transform r_ring2;
    public Transform r_ring3;
    public Transform r_pinky1;//右小拇指
    public Transform r_pinky2;
    public Transform r_pinky3;
    public AvatarTree L_Wrist;
    private AvatarTree L_Thumb1;
    private AvatarTree L_Thumb2;
    private AvatarTree L_Thumb3;
    private AvatarTree L_Index1;
    private AvatarTree L_Index2;
    private AvatarTree L_Index3;
    private AvatarTree L_Middle1;
    private AvatarTree L_Middle2;
    private AvatarTree L_Middle3;
    private AvatarTree L_Ring1;
    private AvatarTree L_Ring2;
    private AvatarTree L_Ring3;
    private AvatarTree L_Pinky1;
    private AvatarTree L_Pinky2;
    private AvatarTree L_Pinky3;
    public AvatarTree R_Wrist;
    private AvatarTree R_Thumb1;
    private AvatarTree R_Thumb2;
    private AvatarTree R_Thumb3;
    private AvatarTree R_Index1;
    private AvatarTree R_Index2;
    private AvatarTree R_Index3;
    private AvatarTree R_Middle1;
    private AvatarTree R_Middle2;
    private AvatarTree R_Middle3;
    private AvatarTree R_Ring1;
    private AvatarTree R_Ring2;
    private AvatarTree R_Ring3;
    private AvatarTree R_Pinky1;
    private AvatarTree R_Pinky2;
    private AvatarTree R_Pinky3;
    public float[] l_datas;
    public float[] r_datas;
    public float lerp;
    private void Start()
    {
        // 获取 BodySolver 的引用
        bodySolver = GetComponent<BodySolver>();
        leftHandLandmarks = null;
        rightHandLandmarks = null;
        BulidTree();
    }
   
    private void BulidTree()
    {
        L_Wrist = new AvatarTree(l_wrist, 5, 0, l_wrist.rotation);//左手
        L_Thumb1 = L_Wrist.childs[0] = new AvatarTree(l_thumb1, 1, 1, l_thumb1.rotation, L_Wrist);
        L_Index1 = L_Wrist.childs[1] = new AvatarTree(l_index1, 1, 5, l_index1.rotation, L_Wrist);
        L_Middle1 = L_Wrist.childs[2] = new AvatarTree(l_middle1, 1, 9, l_middle1.rotation, L_Wrist);
        L_Ring1 = L_Wrist.childs[3] = new AvatarTree(l_ring1, 1, 13, l_ring1.rotation, L_Wrist);
        L_Pinky1 = L_Wrist.childs[4] = new AvatarTree(l_pinky1, 1, 17, l_pinky1.rotation, L_Wrist);
        L_Thumb2 = L_Thumb1.childs[0] = new AvatarTree(l_thumb2, 1, 2, l_thumb2.rotation, L_Thumb1);
        L_Thumb3 = L_Thumb2.childs[0] = new AvatarTree(l_thumb3, 1, 3, l_thumb3.rotation, L_Thumb2);
        L_Index2 = L_Index1.childs[0] = new AvatarTree(l_index2, 1, 6, l_index2.rotation, L_Index1);
        L_Index3 = L_Index2.childs[0] = new AvatarTree(l_index3, 1, 7, l_index3.rotation, L_Index2);
        L_Middle2 = L_Middle1.childs[0] = new AvatarTree(l_middle2, 1, 10, l_middle2.rotation, L_Middle1);
        L_Middle3 = L_Middle2.childs[0] = new AvatarTree(l_middle3, 1, 11, l_middle3.rotation, L_Middle2);
        L_Ring2 = L_Ring1.childs[0] = new AvatarTree(l_ring2, 1, 14, l_ring2.rotation, L_Ring1);
        L_Ring3 = L_Ring2.childs[0] = new AvatarTree(l_ring3, 1, 15, l_ring3.rotation, L_Ring2);
        L_Pinky2 = L_Pinky1.childs[0] = new AvatarTree(l_pinky2, 1, 18, l_pinky2.rotation, L_Pinky1);
        L_Pinky3 = L_Pinky2.childs[0] = new AvatarTree(l_pinky3, 1, 19, l_pinky3.rotation, L_Pinky2);
        R_Wrist = new AvatarTree(r_wrist, 5, 0, r_wrist.rotation);//右手
        R_Thumb1 = R_Wrist.childs[0] = new AvatarTree(r_thumb1, 1, 1, r_thumb1.rotation, R_Wrist);
        R_Index1 = R_Wrist.childs[1] = new AvatarTree(r_index1, 1, 5, r_index1.rotation, R_Wrist);
        R_Middle1 = R_Wrist.childs[2] = new AvatarTree(r_middle1, 1, 9, r_middle1.rotation, R_Wrist);
        R_Ring1 = R_Wrist.childs[3] = new AvatarTree(r_ring1, 1, 13, r_ring1.rotation, R_Wrist);
        R_Pinky1 = R_Wrist.childs[4] = new AvatarTree(r_pinky1, 1, 17, r_pinky1.rotation, R_Wrist);
        R_Thumb2 = R_Thumb1.childs[0] = new AvatarTree(r_thumb2, 1, 2, r_thumb2.rotation, R_Thumb1);
        R_Thumb3 = R_Thumb2.childs[0] = new AvatarTree(r_thumb3, 1, 3, r_thumb3.rotation, R_Thumb2);
        R_Index2 = R_Index1.childs[0] = new AvatarTree(r_index2, 1, 6, r_index2.rotation, R_Index1);
        R_Index3 = R_Index2.childs[0] = new AvatarTree(r_index3, 1, 7, r_index3.rotation, R_Index2);
        R_Middle2 = R_Middle1.childs[0] = new AvatarTree(r_middle2, 1, 10, r_middle2.rotation, R_Middle1);
        R_Middle3 = R_Middle2.childs[0] = new AvatarTree(r_middle3, 1, 11, r_middle3.rotation, R_Middle2);
        R_Ring2 = R_Ring1.childs[0] = new AvatarTree(r_ring2, 1, 14, r_ring2.rotation, R_Ring1);
        R_Ring3 = R_Ring2.childs[0] = new AvatarTree(r_ring3, 1, 15, r_ring3.rotation, R_Ring2);
        R_Pinky2 = R_Pinky1.childs[0] = new AvatarTree(r_pinky2, 1, 18, r_pinky2.rotation, R_Pinky1);
        R_Pinky3 = R_Pinky2.childs[0] = new AvatarTree(r_pinky3, 1, 19, r_pinky3.rotation, R_Pinky2);
    }
    void Update()
    {
        lerp += Time.deltaTime;
        if (lerp >= 1.0f)
        {
            lerp = 0;
        }
        if (bodySolver.isCollectingData)
        {
            // 获取通过 UDP 接收到的数据
            string receivedData = udpReceiver.GetReceivedData();
            if (!string.IsNullOrEmpty(receivedData))
            {
                // 解析数据并设置 poseLandmarks
                LandmarkList llandmarks = udpReceiver.ParseReceivedlefthandData(receivedData);
                if (llandmarks != null)
                {
                    SetLeftHandLandmarks(llandmarks);
                    leftDetected = leftHandLandmarks.landmarks[21].Equals(new Landmark(-1.0f, -1.0f, -1.0f));//相等就是true
                    leftfist = leftHandLandmarks.landmarks[22].Equals(new Landmark(-1.0f, -1.0f, -1.0f));//相等就是握拳  
                    if (leftDetected)//如果等于 [1.0, 1.0, 1.0]，true,就调用 SolveHand("right") 方法。
                    {
                        Debug.Log("Left Hand Detected.");
                        l_datas = ConvertLandmarksToArray(leftHandLandmarks);
                        UpdateTree(L_Wrist, lerp, true);
                    }
                }
                // 解析数据并设置 poseLandmarks
                LandmarkList rlandmarks = udpReceiver.ParseReceivedrighthandData(receivedData);
                if (rlandmarks != null)
                {
                    SetRightHandLandmarks(rlandmarks);
                    rightDetected = rightHandLandmarks.landmarks[21].Equals(new Landmark(-1.0f, -1.0f, -1.0f));
                    rightfist = rightHandLandmarks.landmarks[22].Equals(new Landmark(-1.0f, -1.0f, -1.0f));

                    if (rightDetected)
                    {
                        Debug.Log("Right Hand Detected.");
                        r_datas = ConvertLandmarksToArray(rightHandLandmarks);
                        UpdateTree(R_Wrist, lerp, false);
                    }
                }
            }
        }
    }
    public void SetLeftHandLandmarks(LandmarkList lHandLandmarks)
    {
        leftHandLandmarks = lHandLandmarks;
    }
    public void SetRightHandLandmarks(LandmarkList rHandLandmarks)
    {
        rightHandLandmarks = rHandLandmarks;
    }
    public float[] ConvertLandmarksToArray(LandmarkList HandLandmarks)
    {
        // 获取 Landmark 数组
        Landmark[] landmarks = HandLandmarks.landmarks;

        // 创建一个 float 数组来保存转换后的坐标
        float[] datas = new float[landmarks.Length * 3];

        // 将每个 Landmark 对象的 x, y, z 值添加到 l_datas 数组中
        for (int i = 0; i < landmarks.Length; i++)
        {
            datas[i * 3] = landmarks[i].x;   // x
            datas[i * 3 + 1] = landmarks[i].y; // y
            datas[i * 3 + 2] = landmarks[i].z; // z
        }

        return datas;
    }
    private void UpdateTree(AvatarTree tree, float lerp, bool isLeft)
    {
        if (tree.parent != null)
        {
            //Debug.LogWarning("处理子节点" + tree.idx);
            UpdateBone(tree, lerp, isLeft);
        }
        if (tree.childs != null)
        {
            foreach (var child in tree.childs)
            {
                if (child != null)
                {
                    UpdateTree(child, lerp, isLeft);
                }
            }
        }
    }
    private void UpdateBone(AvatarTree tree, float lerp, bool isLeft)
    {
        Vector3 dir1 = tree.GetDir();
        Vector3 dir2;
        if (isLeft)
        {
            var child_dir = new Vector3(l_datas[tree.idx * 3], l_datas[tree.idx * 3 + 1], -l_datas[tree.idx * 3 + 2]);
            var parent_dir = new Vector3(l_datas[tree.parent.idx * 3], l_datas[tree.parent.idx * 3 + 1], -l_datas[tree.parent.idx * 3 + 2]);
            dir2 = parent_dir - child_dir;
        }
        else
        {
            var child_dir = new Vector3(r_datas[tree.idx * 3], r_datas[tree.idx * 3 + 1], -r_datas[tree.idx * 3 + 2]);
            var parent_dir = new Vector3(r_datas[tree.parent.idx * 3], r_datas[tree.parent.idx * 3 + 1], -r_datas[tree.parent.idx * 3 + 2]);
            dir2 = parent_dir - child_dir;
        }
        dir2.z = -dir2.z;
        Quaternion rot = Quaternion.FromToRotation(dir1, dir2);
        Quaternion rot1 = tree.parent.transf.rotation;
        //tree.parent.transf.rotation = Quaternion.Lerp(rot1, rot * rot1, lerp);
    }//计算关节旋转
}