using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeButton : MonoBehaviour
{
    [SerializeField]
    private Button loadButton; // 按钮
    private UDPReceiver udpReceiver; // 引用 UDPReceiver 脚本

    void Start()
    {
        // 获取 UDPReceiver 脚本组件
        udpReceiver = FindObjectOfType<UDPReceiver>();

        // 给按钮添加点击事件
        loadButton.onClick.AddListener(OnButtonClicked);
    }

    // 按钮点击事件
    void OnButtonClicked()
    {
        // 在切换场景前，停止 UDP 连接
        if (udpReceiver != null)
        {
            udpReceiver.ReleaseResources();
        }
        // 加载场景
        SceneManager.LoadScene("home");
    }
}
