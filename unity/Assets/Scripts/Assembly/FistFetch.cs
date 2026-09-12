using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.Animations;

public class FistFetch : MonoBehaviour
{
    public Hand hand;
    public Transform leftHandTransform;  // 左手手掌的Transform
    public Transform rightHandTransform; // 右手的Transform

    public string targetTag = "Player";  // 目标物体的Tag
    private ParentConstraint parentConstraint;  // 物体上的ParentConstraint

    public bool lefthandFetch;
    public bool righthandFetch;


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
    private void Update()
    {
        lefthandFetch = hand.leftfist;
        righthandFetch = hand.rightfist;
        ConstraintSource source = parentConstraint.GetSource(0);
        if (source.sourceTransform == leftHandTransform)
        {
            if (!lefthandFetch )
            {
                parentConstraint.enabled = false;  // 禁用 ParentConstraint

            }
        }
        if (source.sourceTransform == rightHandTransform)
        {
            if (!righthandFetch)
            {
                parentConstraint.enabled = false;  // 禁用 ParentConstraint

            }
        }
        
    }
    // 当物体进入触发器时
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            // 检查左手部件
            if (other.transform == leftHandTransform && lefthandFetch)
            {
                SetParentConstraintSource(leftHandTransform); // 设置父对象为左手
                parentConstraint.enabled = true;
            }
            // 检查右手部件
            if (other.transform == rightHandTransform && righthandFetch)
            {
                SetParentConstraintSource(rightHandTransform); // 设置父对象为手掌
                parentConstraint.enabled = true;
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
