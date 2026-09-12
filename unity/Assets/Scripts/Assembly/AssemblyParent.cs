using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class AssemblyParent : MonoBehaviour
{
    public FistFetch myScript; // 拖动到 Inspector 中引用脚本
    public string targetTag = "Target"; // 目标物体的Tag
    public Transform targetTransform;  
    private ParentConstraint parentConstraint;  // 物体上的ParentConstraint

    void Start()
    {
        // 获取物体上的 ParentConstraint 组件
        parentConstraint = GetComponent<ParentConstraint>();
    }
    void OnCollisionEnter(Collision collision)
    {
        // 检查碰撞物体的Tag是否匹配
        if (collision.gameObject.CompareTag(targetTag))
        {
            // 判断这个子物体的父物体是不是手
            if (parentConstraint != null && parentConstraint.sourceCount > 0)
            {
                // 获取第一个源
                ConstraintSource source = parentConstraint.GetSource(0);
                // 检查源物体是否是手
                if (source.sourceTransform != null && source.sourceTransform.CompareTag("Player"))
                {
                    SetParentConstraintSource(targetTransform); // 设置父对象
                    myScript.enabled = false;
                }
            }
            
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // 检查离开的物体是否是目标物体
        if (collision.gameObject.CompareTag(targetTag))
        {
            //parentConstraint.enabled = false;
            
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
