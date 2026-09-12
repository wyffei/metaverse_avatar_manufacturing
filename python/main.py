import torch
import os
import cv2
from tqdm import tqdm
import numpy as np
import time
import pyrealsense2 as rs
import matplotlib.pyplot as plt
os.environ['KMP_DUPLICATE_LIB_OK'] = 'True'

from Configs.configs import get_cfg_defaults
from Pose_with_Skeleton.connection_with_unity import Connection_With_Unity,Pose_lib
from Pose_with_Skeleton.body_keypoint_track import Body_Keypoint_Track
from skeleton_optimize.IK_solver import SkeletonIKSolver


def mediapipe_with_optimize(cfg):
    if cfg.UNITY.IS_CONNECT == True:
        connection_with_unity = Connection_With_Unity(
            ip = cfg.UNITY.ADDRESS,
            port = cfg.UNITY.PORT
        )
    if cfg.MEDPIPE.MODE=="cap":
        frame_rate = cfg.CAMERA.RATE
        frame_width, frame_height = cfg.CAMERA.WIDTH, cfg.CAMERA.HEIGHT

        # Configure depth and color streams
        pipeline = rs.pipeline()
        config = rs.config()
        config.enable_stream(rs.stream.depth, frame_width, frame_height, rs.format.z16, frame_rate)
        config.enable_stream(rs.stream.color, frame_width, frame_height, rs.format.bgr8, frame_rate)
        # Start streaming
        cfig = pipeline.start(config)
        # 获取深度流流的配置文件并打印内参
        # depth_profile = cfig.get_stream(rs.stream.depth)
        # depth_intrinsics = depth_profile.as_video_stream_profile().get_intrinsics()
        # print("Depth Intrinsics:", depth_intrinsics)
        # # 从内参中提取值
        # fx = depth_intrinsics.fx
        # fy = depth_intrinsics.fy
        # cx = depth_intrinsics.ppx
        # cy = depth_intrinsics.ppy
        ## 创建后处理滤波器
        # decimation = rs.decimation_filter()  # 降采样
        # spatial = rs.spatial_filter()  # 空间滤波（减少噪声）
        # temporal = rs.temporal_filter()  # 时间滤波（平滑帧间波动）
        # hole_filling = rs.hole_filling_filter()  # 孔洞填充

        body_keypoint_track = Body_Keypoint_Track(
            width=frame_width,
            height=frame_height,
            frame_rate=frame_rate,
            track_hands=cfg.MEDPIPE.TRACK_HANDS,
            track_pose=True,
            smooth_range=5
        )
        # 初始化SkeletonIKSolver
        solver = SkeletonIKSolver(smooth_range=0.3)

        try:
            # # 初始化图形
            # fig = plt.figure()
            # ax = fig.add_subplot(111, projection='3d')
            # # 定义需要连接的点对
            # connections = [(0, 1), (0, 4), (1, 2), (2, 3), (3, 7), (4, 5), (5, 6), (6, 8), (9, 10), (11, 12), (11, 13),
            #                (13, 15), (15, 21), (15, 17), (15, 19), (17, 19),
            #                (12, 14), (14, 16), (16, 22), (16, 18), (16, 22), (16, 20), (20, 18), (11, 23), (12, 24),
            #                (23, 24), (23, 25), (25, 27), (27, 29), (29, 31), (27, 31),
            #                (24, 26), (26, 28), (28, 30), (28, 32), (30, 32)]
            # # connections = [(0, 7), (7, 8), (8, 9), (9, 10), (9, 13), (9, 12), (12, 14), (14, 16), (16, 18),
            # #                (13, 15), (15, 17), (15, 19), (17, 19),
            # #                (0,1), (0,2), (2,4), (4,6), (6, 21), (1,3), (3,5), (5,20)]
            # texts = []
            # # 初始化 scatter 和 lines
            # scatter = ax.scatter([], [], [], c='r', marker='o', label='Valid Points')
            # lines = [ax.plot([], [], [], 'r')[0] for _ in connections]  # 初始化连线
            # # 设置坐标轴范围，调整为适合数据的大小
            # ax.set_xlim([-400, 400])  # 设定 x 轴的范围
            # ax.set_ylim([-400, 400])  # 设定 z 轴的范围
            # ax.set_zlim([-400, 400])  # 设定 y 轴的范围
            # # 反转 y 轴方向
            # ax.set_zlim3d(ax.get_ylim3d()[::-1])
            # # 添加标签和标题
            # ax.set_xlabel('x Label')
            # ax.set_ylabel('z Label')
            # ax.set_zlabel('y Label')
            # ax.set_title('3D Data Visualization with Real-time Updates')
            # ax.legend()

            frame_t = 0.0
            while True:
                # Wait for a coherent pair of frames: depth and color
                frames = pipeline.wait_for_frames()
                # start_time = time.time()  # 记录帧开始时间
                # 深度图
                depth_frame = frames.get_depth_frame()
                # 正常读取的视频流
                color_frame = frames.get_color_frame()
                if not depth_frame or not color_frame:
                    print("Failed to get frame")
                    continue

                # Convert images to numpy arrays
                # 获取原始深度数据
                raw_depth_image = np.asanyarray(depth_frame.get_data())  # 在这一步大了1000倍
                # 应用后处理
                # processed_frame = decimation.process(depth_frame)
                # processed_frame = spatial.process(depth_frame)
                # processed_frame = temporal.process(processed_frame)
                # processed_frame = hole_filling.process(processed_frame)
                # # 获取后处理后的深度数据
                # processed_depth_image = np.asanyarray(processed_frame.get_data())

                color_image = np.asanyarray(color_frame.get_data())
                # 窗口显示原图像
                # color_image1 = np.asanyarray(color_frame.get_data())
                color_image = cv2.cvtColor(color_image, cv2.COLOR_BGR2RGB)

                body_keypoint_track.Track(color_image)

                # Apply colormap on depth image (image must be converted to 8-bit per pixel first)
                # 在深度图像上应用colormap(图像必须先转换为每像素8位)
                # depth_colormap1 = cv2.applyColorMap(cv2.convertScaleAbs(raw_depth_image, alpha=0.05), cv2.COLORMAP_JET)
                # gray_depth = cv2.convertScaleAbs(raw_depth_image, alpha=0.01)
                # depth_colormap2 = cv2.equalizeHist(gray_depth)  # 直方图均衡化
                # depth_colormap2 = cv2.applyColorMap(cv2.convertScaleAbs(processed_depth_image , alpha=0.05), cv2.COLORMAP_JET)

                if body_keypoint_track.img is None:
                    print("No human detected.")
                else:
                    # 遍历每个关键点坐标
                    # for i in range(body_keypoint_track.unity_landmark.shape[0]):
                    # for i in range(22):
                    #     x = int(body_keypoint_track.unity_landmark[i, 0]*cfg.PROCESS.SAMTIMES)
                    #     y = int(body_keypoint_track.unity_landmark[i, 1]*cfg.PROCESS.SAMTIMES)
                    #     # print("x,y(pixel):",x,y)
                    #     # 确保坐标在深度帧的有效范围内
                    #     if 0 <= x < frame_width*cfg.PROCESS.SAMTIMES and 0 <= y < frame_height*cfg.PROCESS.SAMTIMES:
                    #         distance = processed_depth_image[y,x] # 注意坐标顺序为 (行, 列) (240,320)
                    #         Z[i]=distance #缩小倍数，单位mm
                    #         if Z[i] == 0:
                    #             if previous_Z[i] is not None and zero_count < max_zero_count:
                    #                 Z[i] = previous_Z[i]  # 使用上一帧的 Z 值
                    #             else:
                    #                 Z[i] = default_value  # 设定一个默认值，0
                    #                 previous_Z[i] = 0
                    #                 zero_count = 0
                    #             # 增加连续为零的帧数
                    #             zero_count += 1
                    #         else:
                    #             zero_count = 0
                    #
                    #         # 更新前一帧的 Z 值
                    #         previous_Z[i] = Z[i]
                    #         # print("previous_Z:", previous_Z[i])
                    #         # 将像素坐标转换为3D坐标
                    #         X = (x - cx) * Z[i] / fx
                    #         Y = (y - cy) * Z[i] / fy
                    #         # print("{:.0f}: X = {:.2f}, Y = {:.2f}, Z = {:.2f}".format( i ,X, Y, Z))
                    #         body_keypoint_track.unity_landmark[i] =[X,Y,Z[i]] #有时候Z为0，会导致整个点的数据为0。这时使用上一帧的数据。
                    #     else:
                    #         # 如果坐标超出范围，可以设置为无效值（-1）
                    #         body_keypoint_track.unity_landmark[i, 2] = -1
                    #         print(f"point ({i})'s Depth at pixel ({x}, {y}) is invalid")
                    pos = body_keypoint_track.unity_landmark[0]  # 每帧接收到的位置
                    solver.fit_location(torch.tensor(pos), frame_t)  # 更新位置数据
                    body_keypoint_track.unity_landmark[66] = solver.get_smoothed_location(frame_t)
                    hipsx = body_keypoint_track.unity_landmark[66, 0]  # x
                    hipsy = body_keypoint_track.unity_landmark[66, 1]  # y
                    hipsz = body_keypoint_track.unity_landmark[66, 2]  # z
                    # 创建向量
                    vector = np.array([hipsx, hipsy, hipsz])
                    # 计算向量的长度（即点到原点的欧几里得距离）
                    length = np.linalg.norm(vector)
                    # 根据向量长度进行判断和调整
                    while length > 200:
                        # 如果任意一个向量长度大于 200，则调整
                        body_keypoint_track.unity_landmark[66] /= 10
                        length /= 10
                    while length < 10 and length > 0:
                    # 如果任意一个向量长度小于 10，则调整
                        body_keypoint_track.unity_landmark[66] *= 10
                        length *= 10
                    # 打印unity_landmark数组的内容
                    print("Unity Landmark:",body_keypoint_track.unity_landmark)
                    #
                    # # 转换为 NumPy 数组
                    # data_points = body_keypoint_track. unity_landmark_depth[0:33]
                    # data_points = np.array(data_points)
                    # # 提取坐标
                    # x, z, y = data_points[:, 0], data_points[:, 1], data_points[:, 2]
                    # # 每次循环开始时重新计算有效点
                    # valid_mask = np.any(data_points != [0, 0, 0], axis=1)  # 计算有效点
                    # valid_mask[data_points[:, 2] == -1] = False
                    # # 删除当前图中的散点
                    # scatter.remove()
                    # # 只绘制当前有效点
                    # scatter = ax.scatter(x[valid_mask], y[valid_mask], z[valid_mask], c='r', marker='o',
                    #                      label='Valid Points')
                    #
                    # # 更新连线
                    # for line, (start, end) in zip(lines, connections):
                    #     if valid_mask[start] and valid_mask[end]:
                    #         line.set_data([x[start], x[end]], [y[start], y[end]])
                    #         line.set_3d_properties([z[start], z[end]])
                    #     else:
                    #         line.set_data([], [])
                    #         line.set_3d_properties([])
                    # # 删除之前的标号
                    # for text in texts:
                    #     text.remove()
                    # # 添加新的点的标号
                    # texts = []  # 清空文本对象列表
                    # for i, point in enumerate(data_points):
                    #     if not valid_mask[i]:
                    #         continue  # 跳过无效点
                    #     # 添加文本标号
                    #     text = ax.text(point[1], point[2], point[0], f"{i}", color='blue', fontsize=8)
                    #     texts.append(text)  # 存储文本对象
                    # plt.draw()
                    # plt.pause(0.1)  # 暂停100ms，避免占用过多CPU
                    cv2.imshow('MediaPipe', body_keypoint_track.img[:, :, ::-1])  # 显示处理后画面
                    # cv2.imshow("Raw DepthProcessed Depth", depth_colormap1)
                    # cv2.imshow('Processed Depth',depth_colormap2 )  # 显示处理后画面

                if cv2.waitKey(1) & 0xFF == 27:
                    break
                try:
                    frame_t += 1.0 / frame_rate
                except ZeroDivisionError:
                    frame_rate = 30
                    frame_t += 1.0 / frame_rate
                try:
                    connection_with_unity.send_info_to_unity(body_keypoint_track)
                except Exception as e:
                    print(f"发送数据时发生异常: {e}")
                    continue
            # plt.show()
        finally:
            # Stop streaming
            pipeline.stop()


if __name__ =="__main__":
    cfg = get_cfg_defaults()
    # mediapipe_detect(cfg)
    mediapipe_with_optimize(cfg)