using BodyTracking;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class ArmLegSolver : MonoBehaviour
{
    [SerializeField] private UDPReceiver udpReceiver;  // 通过 inspector 链接到 UdpReceiver
    private BodySolver bodySolver;

    const float SMOOTHING = 0.1f;   // lower value = smoother, but less responsive

    // landmark indices
    const int LEFTSHOULDER = 14;
    const int RIGHTSHOULDER = 15;
    const int LEFTELBOW = 16;
    const int RIGHTELBOW = 17;
    const int LEFTWRIST = 18;
    const int RIGHTWRIST = 19;

    /*const int LEFTUPPERLEG = 1;
    const int RIGHTUPPERLEG = 2;
    const int LEFTLOWERLEG = 3;
    const int RIGHTLOWERLEG = 4;
    const int LEFTFOOT = 5;
    const int RIGHTFOOT = 6;
    const int LEFTTOE = 20;
    const int RIGHTTOE = 21;*/



    // 使用 [SerializeField] 来暴露给 Inspector，便于在编辑器中拖拽赋值

    [SerializeField] private Transform hips;
    [SerializeField] private bool mirrorMode;

    //[SerializeField] private Transform spine;
    //[SerializeField] private Transform spine1;
    //[SerializeField] private Transform spine2;

    [SerializeField] private Transform lShoulderTf;
    [SerializeField] private Transform lElbowTf;
    [SerializeField] private Transform lWristTf;
    //[SerializeField] private Transform lFingerTf;
    [SerializeField] private Transform rShoulderTf;
    [SerializeField] private Transform rElbowTf;
    [SerializeField] private Transform rWristTf;
    //[SerializeField] private Transform rFingerTf;

    [SerializeField] private Transform lUlegTf;
    [SerializeField] private Transform lLlegTf;
    [SerializeField] private Transform lFootTf;
    [SerializeField] private Transform lToeTf;
    [SerializeField] private Transform rUlegTf;
    [SerializeField] private Transform rLlegTf;
    [SerializeField] private Transform rFootTf;
    [SerializeField] private Transform rToeTf;



    private (Transform, Transform)[] transformPairs;
    private (int, int)[] landmarkIdxPairs;

    private LandmarkList poseLandmarks;
    private Quaternion[] prevRots;



    void Start()
    {
        // 获取 BodySolver 的引用
        bodySolver = GetComponent<BodySolver>();
        if (!mirrorMode)
        {
            (lShoulderTf, rShoulderTf) = (rShoulderTf, lShoulderTf);
            (lElbowTf, rElbowTf) = (rElbowTf, lElbowTf);
            (lWristTf, rWristTf) = (rWristTf, lWristTf);

            (lUlegTf, rUlegTf) = (rUlegTf, lUlegTf);
            (lLlegTf, rLlegTf) = (rLlegTf, lLlegTf);
            (lFootTf, rFootTf) = (rFootTf, lFootTf);
            (lToeTf, rToeTf) = (rToeTf, lToeTf);


        }

        transformPairs = new (Transform, Transform)[]{ //(hips,spine),(spine,spine1),(spine1,spine2),
                                                       (rShoulderTf, rElbowTf), (rElbowTf, rWristTf), 
                                                       (lShoulderTf, lElbowTf), (lElbowTf, lWristTf), 
                                                       //(hips,rUlegTf), (hips,lUlegTf),
                                                       (rUlegTf, rLlegTf), (rLlegTf, rFootTf), (rFootTf, rToeTf),
                                                       (lUlegTf, lLlegTf), (lLlegTf, lFootTf), (lFootTf, lToeTf)
                                                       };
        landmarkIdxPairs = new (int, int)[]{ //(HIPS,SPINE),(SPINE,CHEST),(CHEST,UPPERCHEST),
                                             (LEFTSHOULDER, LEFTELBOW), (LEFTELBOW, LEFTWRIST), 
                                             (RIGHTSHOULDER, RIGHTELBOW), (RIGHTELBOW, RIGHTWRIST), 
                                             //(LEFTUPPERLEG, LEFTLOWERLEG), (LEFTLOWERLEG, LEFTFOOT), (LEFTFOOT, LEFTTOE),
                                             //(RIGHTUPPERLEG, RIGHTLOWERLEG), (RIGHTLOWERLEG, RIGHTFOOT), (RIGHTFOOT, RIGHTTOE)
                                             };

        poseLandmarks = null;

        prevRots = new Quaternion[transformPairs.Length];
        for (int i = 0; i < prevRots.Length; i++)
        {
            prevRots[i] = transformPairs[i].Item1.localRotation;
        }

    }

    void Update()
    {
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

        // 旋转以更改初始姿态为T形
        lUlegTf.localRotation = Quaternion.Euler(180, 0, 0);
        //lUlegTf.localRotation = Quaternion.Euler((float)-2.47, (float)-256.48, (float)-182.29);

        if (poseLandmarks != null && bodySolver.isCollectingData) SolvePose();
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

        Vector3 lShoulderLm = new Vector3(-poseLandmarks.landmarks[LEFTSHOULDER].x, -poseLandmarks.landmarks[LEFTSHOULDER].y, poseLandmarks.landmarks[LEFTSHOULDER].z);
        Vector3 rShoulderLm = new Vector3(-poseLandmarks.landmarks[RIGHTSHOULDER].x, -poseLandmarks.landmarks[RIGHTSHOULDER].y, poseLandmarks.landmarks[RIGHTSHOULDER].z);
        //Vector3 lUPPERLEGLm = new Vector3(-poseLandmarks.landmarks[LEFTUPPERLEG].x, -poseLandmarks.landmarks[LEFTUPPERLEG].y, poseLandmarks.landmarks[LEFTUPPERLEG].z);
        //Vector3 rUPPERLEGLm = new Vector3(-poseLandmarks.landmarks[RIGHTUPPERLEG].x, -poseLandmarks.landmarks[RIGHTUPPERLEG].y, poseLandmarks.landmarks[RIGHTUPPERLEG].z);

        if (!mirrorMode)
        {
            lShoulderLm.z *= -1;
            rShoulderLm.z *= -1;
            //lUPPERLEGLm.z *= -1;
            //rUPPERLEGLm.z *= -1;
        }

        Vector3 v_ShoulderLm = (rShoulderLm - lShoulderLm).normalized;
        Vector3 v_ShoulderTf = (lShoulderTf.position - rShoulderTf.position).normalized;
        Quaternion rot1 = Quaternion.FromToRotation(v_ShoulderLm, v_ShoulderTf);

        // convert Landmarks to Vector3s, with shoulders aligned with avatar
        Vector3[] landmarks = new Vector3[poseLandmarks.landmarks.Length];
        for (int i = 14; i < 22; i++)
        {
            /*
            landmark coordinate frame
               x-axis: left to right
               y-axis: bottom to top
               z-axis: into the screen
            */
            landmarks[i] = new Vector3(-poseLandmarks.landmarks[i].x, -poseLandmarks.landmarks[i].y, poseLandmarks.landmarks[i].z);  // 访问 landmarks 数组
            if (!mirrorMode) landmarks[i].z *= -1;
            //Debug.Log($"Index: {i}, Landmark: {landmarks[i]}");
            landmarks[i] = rot1 * landmarks[i];

        }
        /* Vector3 v_UPPERLEGLm = (rUPPERLEGLm - lUPPERLEGLm).normalized;
         Vector3 v_UPPERLEGTf = (lUlegTf.position - rUlegTf.position).normalized;
         Quaternion rot2 = Quaternion.FromToRotation(v_UPPERLEGLm, v_UPPERLEGTf);
         // convert Landmarks to Vector3s, with shoulders aligned with avatar
         for (int i = 1; i < 7; i++)
         {

             landmarks[i] = new Vector3(-poseLandmarks.landmarks[i].x, -poseLandmarks.landmarks[i].y, poseLandmarks.landmarks[i].z);  // 访问 landmarks 数组
             if (!mirrorMode) landmarks[i].z *= -1;
             //确保 landmarks 的左右腿坐标转换到模型对应的左右腿坐标方向，保持 landmarks 数据与 Unity 模型的一致性。
             //Debug.Log($"Index: {i}, Landmark: {landmarks[i]}");
             landmarks[i] = rot2 * landmarks[i];

         }*/
        // solve rotations

        for (int i = 0; i < landmarkIdxPairs.Length; i++)
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
            parentTf.localRotation = Quaternion.Slerp(rotOld, rotNew, SMOOTHING);
            prevRots[i] = parentTf.localRotation;
        }
        /*Quaternion LeftUpperLegRotation = Quaternion.identity;
        Quaternion RightUpperLegRotation = Quaternion.identity;
        for (int i = 4; i < 6; i++) //4 5
        {
            (Transform parentTf, Transform childTf) = transformPairs[i];

            // smooth, interpolated rotation           
            Quaternion rotOld = prevRots[i];

            Quaternion rotNew = bodySolver.overallRotation;


            // 这里不需要每次重新声明四元数，直接赋值
            if (i == 4) 
            { 
                LeftUpperLegRotation = rotNew;
                // 将计算出的整体旋转应用到根骨骼（hips）
                lUlegTf.rotation = Quaternion.Slerp(rotOld, LeftUpperLegRotation, SMOOTHING);
            }
            if (i == 5) 
            { 
                RightUpperLegRotation = rotNew;
                // 将计算出的整体旋转应用到根骨骼（hips）
                rUlegTf.rotation = Quaternion.Slerp(rotOld, LeftUpperLegRotation, SMOOTHING);
            }

            prevRots[i] = parentTf.localRotation;
        }*/

    }



}

