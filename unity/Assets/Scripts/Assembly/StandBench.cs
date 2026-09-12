using System.Collections;
using UnityEngine;

public class StandBench : MonoBehaviour
{
    public Vector2 rangeX; // x 坐标范围，(270,650)
    public Vector2 rangeZ; // z 坐标范围，(150,385)
    [SerializeField] public Vector3 currentPosition;
    public Vector3 targetPosition; // (465,0,390)
    public Animator animator; // 模型的动画组件
    public float moveSpeed = 0.05f; // 移动速度

    [SerializeField] public bool isMoving = false; // 是否正在移动
    [SerializeField] public bool isInRange = false; // 是否进入范围

    private Rigidbody rb;

    void Start()
    {

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        currentPosition = transform.position;

        // 检测是否进入范围
        if (!isInRange &&
            currentPosition.x >= rangeX.x && currentPosition.x <= rangeX.y &&
            currentPosition.z >= rangeZ.x && currentPosition.z <= rangeZ.y)
        {
            isInRange = true;
            StartCoroutine(MoveToTarget());
        }
    }

    IEnumerator MoveToTarget()
    {
        isMoving = true;

        // 启动动画
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("isWalking", true);
        }

        // 移动到目标点
        while (Vector3.Distance(transform.position, targetPosition) > 1f)
        {
            // 计算平滑的目标位置
            Vector3 smoothPosition = Vector3.Lerp(transform.position, targetPosition, moveSpeed);
            // 使用物理引擎平滑移动
            rb.MovePosition(smoothPosition);

            // 计算目标位置的方向
            Vector3 direction = (targetPosition - transform.position).normalized;
            // 设置朝向目标位置
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            yield return null;
        }

        // 确保到达目标点
        transform.position = targetPosition;


        // 停止动画
        animator.enabled = false;

        // 设置的朝向为 z 轴方向
        Vector3 negativeXAxis = Vector3.forward; // z 方向
        transform.rotation = Quaternion.LookRotation(negativeXAxis, Vector3.up); // 使用世界坐标系中的 y 轴作为向上方向

        isMoving = false;
    }
}
