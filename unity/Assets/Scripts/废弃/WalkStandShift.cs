using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkStandShift : MonoBehaviour
{
    Animator animator;

    // 人物移动速度（可调节）
    public float MoveSpeed = 1.5f;

    // 控制是否行走的变量
    public bool hipsshift;

    // 当前 hips 目标位置（假设这是从 landmarks[66] 获得的目标坐标）
    public Vector3 targetHipsPosition;

    // 引用 BodySolver 脚本
    private BodySolver bodySolver;

    // 表示角色接近目标位置的最小距离（避免模型一直移动）
    public float maxMoveDistance = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        // 获取“动画制作者组件”
        animator = GetComponent<Animator>();

        // 获取 BodySolver 的引用（假设它挂载在同一个物体上）
        bodySolver = GetComponent<BodySolver>();

        // 初始化动画状态变量为0（人物静止动画播放）
        hipsshift = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 获取 BodySolver 中的目标 hips 位置
        targetHipsPosition = bodySolver.hipsNowPosition;

        // 控制角色移动
        Control_Move();
    }

    public void Control_Move()
    {
        // 如果 hipsshift 为 true，角色开始移动
        if (hipsshift)
        {
            // 启用走路动画
            animator.SetBool("HipsShift", hipsshift);

            // 计算当前位置和目标位置的差值（即目标方向）
            Vector3 moveDirection = targetHipsPosition - transform.position;

            // 如果目标位置与当前位置不一样
            if (moveDirection != Vector3.zero)
            {
                // 计算出目标的方向并归一化
                moveDirection.Normalize();

                // 使用目标的方向来平移角色，不受当前旋转影响
                Vector3 move = moveDirection * MoveSpeed * Time.deltaTime;

                // 使用 transform.Translate 来平移角色，注意这是世界坐标（Space.World）
                //transform.Translate(move, Space.World);
            }

            // 如果目标距离小于设定的最大移动距离，则停止移动
            if (Vector3.Distance(transform.position, targetHipsPosition) <= maxMoveDistance)
            {
                hipsshift = false;  // 到达目标后停止走路动画
                animator.SetBool("HipsShift", false);
            }
        }
        else
        {
            // 如果 hipsshift 为 false，角色站立不动
            animator.SetBool("HipsShift", false);
        }
    }
}
