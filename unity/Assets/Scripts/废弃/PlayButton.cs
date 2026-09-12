using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 引用 UnityEngine.UI 命名空间

public class PlayButton : MonoBehaviour
{

    [SerializeField]
    private Button playButton;       // 在 Inspector 中引用播放按钮
    //private bool isPlaying = false;  // 是否正在播放

    // Start is called before the first frame update
    void Start()
    {
        // 给按钮添加点击监听事件
        playButton.onClick.AddListener(OnPlayButtonClick);
    }

    // 按钮点击事件，开始播放数据
    private void OnPlayButtonClick()
    {

        //isPlaying = true;
        Debug.Log("Started playing data.");

    }
}
