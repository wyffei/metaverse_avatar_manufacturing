using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WalkAnimation : MonoBehaviour
{
    Animator animator;

    // 人物移动速度（可调节）
    public float MoveSpeed = 0.1f;
    public float rotationSpeed = 5f;


    // 当前 hips 目标位置（假设这是从 landmarks[66] 获得的目标坐标）
    private Vector3 targetHipsPosition;


    private BodySolver bodySolver;
    private StandBench standBench;
    private Rigidbody rb;

    // 表示角色接近目标位置的最小距离（避免模型一直移动）
    public float maxMoveDistance = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        // 获取“动画制作者组件”
        animator = GetComponent<Animator>();

        // 获取 BodySolver 的引用（假设它挂载在同一个物体上）
        bodySolver = GetComponent<BodySolver>();
        standBench = GetComponent<StandBench>();
        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {

            // 获取 BodySolver 中的目标 hips 位置
            targetHipsPosition = bodySolver.hipsNowPosition;
            if (targetHipsPosition != Vector3.zero && !standBench.isInRange)
            {
                // 控制角色移动
                Control_Move();
            }

    }

    public void Control_Move()
    {

        Vector3  moveDirection = targetHipsPosition - transform.position;
        moveDirection.y= 0;
        Debug.Log("moveDirection:" + moveDirection+"="+ "hipsNowPosition:" + targetHipsPosition +"-"+ "moveDirection:"+ transform.position);
            // 如果目标位置与当前位置不一样
            if (moveDirection != Vector3.zero && rb != null && animator.enabled == true)
            {
                // 计算出目标的方向并归一化
                moveDirection.Normalize();

                // 获取目标方向的旋转角度（绕 y 轴旋转）
                float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;  // 转换为度数

                // 创建一个旋转，沿着 y 轴旋转到目标方向
                Quaternion targetRotation = Quaternion.Euler(0, angle, 0);

            // 使用物理引擎平滑旋转
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));

            // 计算平滑的目标位置
            Vector3 smoothPosition = Vector3.Lerp(transform.position, targetHipsPosition, MoveSpeed);

            // 使用物理引擎平滑移动
            rb.MovePosition(smoothPosition);

            // 控制角色的实际移动，使用移动方向 * 移动速度来决定速度
            //float moveSpeedFactor = MoveSpeed * Time.deltaTime;  // 控制移动速度
            //transform.position += moveDirection * moveSpeedFactor;

        }
    }
}