using UnityEngine;
using UnityEngine.Animations;

public class FetchItem : MonoBehaviour
{
    public Transform leftHandTransform;  // 左手手掌的Transform
    public Transform leftHandTransform1;  // 左手1的Transform
    public Transform leftHandTransform2;  // 左手2的Transform
    public Transform leftHandTransform3;  // 左手3的Transform

    public Transform rightHandTransform; // 右手的Transform
    public Transform rightHandTransform1; // 右手的Transform
    public Transform rightHandTransform2; // 右手的Transform
    public Transform rightHandTransform3; // 右手的Transform

    public string targetTag = "TargetObject";  // 目标物体的Tag
    private ParentConstraint parentConstraint;  // 物体上的ParentConstraint
    private Transform currentHandTransform;  // 当前控制物体的手部Transform

    private bool leftHandPartInTrigger = false;  // 左手手掌是否进入触发器
    private bool leftHandPart1InTrigger = false;  // 左手部件1是否进入触发器
    private bool leftHandPart2InTrigger = false;  // 左手部件2是否进入触发器
    private bool leftHandPart3InTrigger = false;  // 左手部件1是否进入触发器
    private bool rightHandPartInTrigger = false;  
    private bool rightHandPart1InTrigger = false;
    private bool rightHandPart2InTrigger = false;
    private bool rightHandPart3InTrigger = false;

    void Start()
    {
        // 获取物体上的 ParentConstraint 组件
        parentConstraint = GetComponent<ParentConstraint>();

        // 确保 ParentConstraint 一开始是禁用的
        if (parentConstraint != null)
        {
            parentConstraint.enabled = false;
        }
    }

    // 当物体进入触发器时
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            bool shouldEnableParentConstraint = false;

            // 检查左手部件
            if (other.transform == leftHandTransform)
            {
                leftHandPartInTrigger = true;
                Debug.LogWarning("0");
            }
            if (other.transform == leftHandTransform1)
            {
                leftHandPart1InTrigger = true;
                Debug.LogWarning("1");
            }
            if (other.transform == leftHandTransform2)
            {
                leftHandPart2InTrigger = true;
                Debug.LogWarning("2");
            }
            if (other.transform == leftHandTransform3)
            {
                leftHandPart3InTrigger = true;
                Debug.LogWarning("3");
            }
            if (leftHandPart1InTrigger && leftHandPart2InTrigger && leftHandPart3InTrigger && leftHandPartInTrigger)
            {
                shouldEnableParentConstraint = true;
                SetParentConstraintSource(leftHandTransform); // 设置父对象为左手手掌
            }
            // 检查右手部件
            if (other.transform == rightHandTransform)
            {
                rightHandPartInTrigger = true;
            }
            if (other.transform == rightHandTransform1)
            {
                rightHandPart1InTrigger = true;
            }
            if (other.transform == rightHandTransform2)
            {
                rightHandPart2InTrigger = true;
            }
            if (other.transform == rightHandTransform3)
            {
                rightHandPart3InTrigger = true;
            }
            if (rightHandPart1InTrigger && rightHandPart2InTrigger && rightHandPart3InTrigger && rightHandPartInTrigger)
            {
                shouldEnableParentConstraint = true;
                SetParentConstraintSource(rightHandTransform); // 设置父对象为手掌
            }

            // 仅当满足条件并且 ParentConstraint 还未启用时，启用 ParentConstraint
            if (shouldEnableParentConstraint && parentConstraint != null && !parentConstraint.enabled)
            {
                parentConstraint.enabled = true;
                Debug.LogError("目标物体跟随手移动");
            }
        }
    }


    // 当物体离开触发器时
    void OnTriggerExit(Collider other)
    {
        // 检查是否是目标物体
        if (other.CompareTag(targetTag))
        {
            if (other.transform == leftHandTransform1)
            {
                leftHandPart1InTrigger = false; // 左手1离开触发器
                Debug.LogWarning("1-");
            }
            if (other.transform == leftHandTransform2)
            {
                leftHandPart2InTrigger = false; // 左手2离开触发器
                Debug.LogWarning("2-");
            }
            if (other.transform == leftHandTransform3)
            {
                leftHandPart3InTrigger = false; // 左手3离开触发器
                Debug.LogWarning("3-");
            }
            if (!leftHandPart1InTrigger && !leftHandPart2InTrigger && !leftHandPart3InTrigger )
            {
                leftHandPartInTrigger = false;
                parentConstraint.enabled = false;  // 禁用 ParentConstraint
                Debug.LogWarning("目标物体不跟随左手");
            }

            // 如果是右手
            if (other.transform == rightHandTransform1)
            {
                rightHandPart1InTrigger = false; // 左手1离开触发器
            }
            if (other.transform == rightHandTransform2)
            {
                rightHandPart2InTrigger = false; // 左手2离开触发器
            }
            if (other.transform == rightHandTransform3)
            {
                rightHandPart3InTrigger = false; // 左手3离开触发器
            }
            if (!rightHandPart1InTrigger && !rightHandPart2InTrigger && !rightHandPart3InTrigger)
            {
                rightHandPartInTrigger = false;
                parentConstraint.enabled = false;  // 禁用 ParentConstraint
            }

        }
    }

    // 设置物体的 ParentConstraint 源
    void SetParentConstraintSource(Transform handTransform)
    {
        if (parentConstraint != null && parentConstraint.sourceCount > 0)
        {
            // 获取源并设置为触碰的手
            ConstraintSource source = parentConstraint.GetSource(0);
            source.sourceTransform = handTransform;
            source.weight = 1f;  // 设置第1个源的权重为1，1完全跟随
            parentConstraint.SetSource(0, source);
        }
    }

}
