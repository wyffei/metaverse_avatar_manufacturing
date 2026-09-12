# Metaverse Avatar Manufacturing

基于深度相机 + MediaPipe 的实时动作捕捉系统：采集真人姿态/手势并驱动 Unity 中的虚拟人（Avatar），用于虚拟装配作业训练与动作质量评估（AQA）。

## 项目组成

项目分为两部分，通过 UDP 通信：

```
python/   动作捕捉与骨骼解算端（采集 -> 识别 -> 优化 -> 发送）
unity/    虚拟人驱动与装配训练场景端（接收 -> 驱动骨骼 -> 场景交互 -> 动作评估）
```

数据流：RealSense 相机 → MediaPipe 姿态/手势识别 → 卡尔曼滤波 / IK 优化 → UDP 发送坐标 → Unity 接收并驱动虚拟人骨骼。

## python/ 动作捕捉端

| 目录 | 说明 |
| --- | --- |
| `main.py` | 主入口，读取 RealSense 深度/彩色流，串联识别与发送流程 |
| `Pose_with_Skeleton/body_keypoint_track.py` | 基于 MediaPipe Pose/Hands 提取全身与手部关键点，卡尔曼滤波去抖，映射为 Unity 所需的 69 点骨骼数组 |
| `Pose_with_Skeleton/connection_with_unity.py` | 通过 UDP socket 把骨骼数据发送给 Unity |
| `hand_gesture/hand_recognise.py` | 手势识别（握拳/张开判断） |
| `skeleton_optimize/` | IK 求解（`IK_solver.py`）、MLS/卡尔曼平滑等骨骼优化算法 |
| `Configs/configs.py` | 相机参数、Unity 连接地址端口（默认 `127.0.0.1:9600`）等配置 |

### 环境依赖

- Python 3.9+
- Intel RealSense 深度相机 + `pyrealsense2`
- 依赖见 [python/requirements.txt](python/requirements.txt)（mediapipe、opencv、torch、tensorflow 等）

### 运行

```bash
cd python
pip install -r requirements.txt
python main.py
```

需先启动 Unity 场景等待 UDP 连接，再运行 `main.py`。

## unity/ 虚拟人与训练场景端

- Unity 版本：`2021.3.34f1`
- 用 Unity Hub 打开 `unity/` 目录即可

| 目录 | 说明 |
| --- | --- |
| `Assets/Scripts/UI/UDPReceiver.cs` | 接收 Python 发送的骨骼坐标数据 |
| `Assets/Scripts/MotionCapture/` | 将关键点数据解算为虚拟人骨骼动作（BodySolver、ArmLegSolver、Hand、Landmark） |
| `Assets/Scripts/Assembly/` | 装配训练场景交互逻辑（抓取零件、行走、站立操作台等） |
| `Assets/Scripts/AQA/` + `Assets/Python_AQA/` | 动作质量评估（Action Quality Assessment）：基于 DTW / 欧式距离 / 马氏距离 / Autoencoder 等方法，比对采集动作与标准动作的相似度 |
| `Assets/Scenes/` | `home.unity`（主菜单）、`novice.unity`（新手训练）、`skilled.unity`（熟练工训练） |

## 其他说明


- Unity 的 `Library/`、`obj/`、`Logs/`、`.vs/`、`UserSettings/` 等目录为编辑器自动生成的缓存，已在 `.gitignore` 中排除，首次用 Unity 打开工程时会自动重建。
