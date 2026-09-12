import mediapipe as mp
import numpy as np
import cv2
from collections import deque
from skeleton_optimize.optimize import mls_smooth_numpy,KalmanFilterWrapper
from Configs.Skleleton_config import MEDIAPIPE_KEYPOINTS_WITHOUT_HANDS,WEIGHTS,MEDIAPIPE_KEYPOINTS_HANDS
from hand_gesture.hand_recognise import predict_fist
import cv2
class Body_Keypoint_Track:
    def __init__(self, width:int, height:int, frame_rate: int, track_hands = True, track_pose = True,smooth_range = 5):
        self.width = width
        self.height = height
        self.track_hands = track_hands
        self.track_pose = track_pose
        self.pose_2d, self.pose_3d, self.pose_vis = None, None, None
        self.mp_pose = mp.solutions.pose.Pose(
            model_complexity=1, #default to 1, val from 0 to 2,
            static_image_mode=False, ##video stream, only detect the first very prominent image and track the landmarks until lose
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5,
            smooth_landmarks=True,
        )
        self.left_hands_2d, self.right_hands_2d= None, None
        self.mp_hands_holistic = mp.solutions.holistic.Holistic(
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5,
            model_complexity=2,
            static_image_mode=False,
            enable_segmentation=True
        )
        self.mp_hands_hands = mp.solutions.hands.Hands(
            static_image_mode=False,
            min_tracking_confidence=0.5,
            min_detection_confidence=0.5,
            max_num_hands=2,
        )
        self.world_landmark = None
        self.origin_pos = None
        self.temp_world_landmark = None
        self.unity_landmark = None
        self.img = None
        self.smooth_range = smooth_range
        #---------use stack-----------------
        self.queue = []
        # init the calman=========
        #--init body--------
        self.kf_body =KalmanFilterWrapper(33*3,0.05,0.03,0.05)
        # init left hand------
        self.kf_lefthand =KalmanFilterWrapper(21*3,0.1,0.1,0.05)
        # init right hand------
        self.kf_righthand =KalmanFilterWrapper(21*3,0.1,0.1,0.05)

        self.right_predict_result,self.left_predict_result=None,None
        self.unity_landmark_dept=None
        self.pose_vis_depth = None

    def Track_pose(self, image):
        self.pose_2d, self.pose_3d = None, None
        results = self.mp_pose.process(image)
        if results.pose_landmarks is None:
            return
        # 2d
        tmp_2d_ldmk = np.array(
            [[a.x * self.width, a.y * self.height, a.z * self.width] for a in results.pose_landmarks.landmark])
        self.pose_vis_depth = np.array([a.visibility for a in results.pose_landmarks.landmark])  # 一维数组
        # 3d
        tmp_3d_ldmk = np.array([[a.x , a.y , a.z] for a in results.pose_world_landmarks.landmark])
        visiable = np.array([[a.visibility] for a in results.pose_landmarks.landmark])          

        self.img = image
        mp.solutions.drawing_utils.draw_landmarks(
            image=self.img,
            landmark_list=results.pose_landmarks,
            connections=mp.solutions.pose.POSE_CONNECTIONS,
            landmark_drawing_spec=mp.solutions.drawing_utils.DrawingSpec(color=(0, 0, 0), thickness=1, circle_radius=2),
            connection_drawing_spec=mp.solutions.drawing_utils.DrawingSpec(color=(255, 255, 255), thickness=2,
                                                                           circle_radius=2)
        )
        #self.barycenter = np.average(tmp_2d_ldmk, axis=0, weights=self.barycenter_weight)
        #self.barycenter_history.append(self.barycenter)
        self.pose_2d, self.pose_3d, self.pose_vis = tmp_2d_ldmk, tmp_3d_ldmk, visiable
        #--init the KalmanFilter Initial state-----------------
        if not hasattr(self.kf_body.kf, 'statePost') and self.pose_2d:
            self.kf_body.statapose=np.array(self.pose_2d,dtype=np.float32).flatten()
        #--use KalmanFilter to predict
        self.kf_body.predict()
        #--fixed of input coordinate
        self.kf_body.filter(np.array(self.pose_2d,dtype=np.float32).flatten())
        #--the result of prediction
        self.pose_2d = self.kf_body.kf.statePost.reshape(-1,3)

    # # mp.solutions.holistic.Holistic
    # def Track_hands_holistic(self, image):
    #     self.left_hands_2d, self.right_hands_2d= None, None
    #     results = self.mp_hands_holistic.process(image)
    #
    #     if (results.left_hand_landmarks is None) and (results.right_hand_landmarks is None):
    #         return
    #
    #     if results.left_hand_landmarks:
    #         mp.solutions.drawing_utils.draw_landmarks(
    #             image, results.left_hand_landmarks, mp.solutions.holistic.HAND_CONNECTIONS,
    #             mp.solutions.drawing_utils.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=4),
    #             mp.solutions.drawing_utils.DrawingSpec(color=(0, 0, 255), thickness=2, circle_radius=2),
    #         )
    #         # Each landmark consists of x, y and z.
    #         # x and y are normalized to[0.0, 1.0] by the image width and height respectively.
    #         # z represents the landmark depth with the depth at the wrist being the origin,
    #         # and the smaller the value the closer the landmark is to the camera. The magnitude of z uses roughly the same scale as x.
    #         self.left_hands_2d = np.array(
    #             [[a.x, a.y, a.z] for a in results.left_hand_landmarks.landmark])
    #
    #     if results.right_hand_landmarks:
    #         self.right_hands_2d = np.array(
    #             [[a.x, a.y, a.z] for a in results.right_hand_landmarks.landmark])
    #         mp.solutions.drawing_utils.draw_landmarks(
    #             image, results.right_hand_landmarks, mp.solutions.holistic.HAND_CONNECTIONS,
    #             mp.solutions.drawing_utils.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=4),
    #             mp.solutions.drawing_utils.DrawingSpec(color=(0, 0, 255), thickness=2, circle_radius=2),
    #         )


    # mp.solutions.hands.Hands
    def Track_hands_hands(self, image):
        self.left_hands_2d, self.right_hands_2d= None, None
        results = self.mp_hands_hands.process(image)

        if results.multi_handedness is None:
            return

        NumsOfHands = len(results.multi_handedness)
        left_hand_id = list(filter(lambda i: results.multi_handedness[i].classification[0].label == 'Right', range(NumsOfHands)))
        right_hand_id = list(filter(lambda i : results.multi_handedness[i].classification[0].label == "Left", range(NumsOfHands)))
        if len(left_hand_id) > 0:
            left_hand_id = left_hand_id[0]
            self.left_predict_result = predict_fist(self.width, self.height,results.multi_hand_landmarks[left_hand_id])
            # print(self.left_predict_result)
            self.left_hands_2d = np.array([[a.x, a.y, a.z] for a in results.multi_hand_landmarks[left_hand_id].landmark])

            mp.solutions.drawing_utils.draw_landmarks(
            image=self.img,
            landmark_list=results.multi_hand_landmarks[left_hand_id],
            connections=mp.solutions.hands.HAND_CONNECTIONS,
            landmark_drawing_spec=mp.solutions.drawing_utils.DrawingSpec(color=(0, 0, 0), thickness=1, circle_radius=2),
            connection_drawing_spec=mp.solutions.drawing_utils.DrawingSpec(color=(0, 0,255), thickness=2,
                                                                            circle_radius=2)
            )
        if len(right_hand_id) > 0:
            right_hand_id = right_hand_id[0]
            self.right_predict_result = predict_fist( self.width, self.height, results.multi_hand_landmarks[right_hand_id])
            self.right_hands_2d = np.array(
                [[a.x, a.y , a.z] for a in results.multi_hand_landmarks[right_hand_id].landmark])

            mp.solutions.drawing_utils.draw_landmarks(
            image=self.img,
            landmark_list=results.multi_hand_landmarks[right_hand_id],
            connections=mp.solutions.hands.HAND_CONNECTIONS,
            landmark_drawing_spec=mp.solutions.drawing_utils.DrawingSpec(color=(0, 0, 0), thickness=1, circle_radius=2),
            connection_drawing_spec=mp.solutions.drawing_utils.DrawingSpec(color=(0, 0,255), thickness=2,
                                                                            circle_radius=2)
            )



    def optimizer(self):
        if self.pose_2d is None:
            return
        x = np.array([x > 0.2 for x in self.pose_vis])
        if x.sum() < 10:
            self.unity_landmark = None
            return
        #===use slide window filter to optimize bones====================
        #slide window filter
        # if len(self.queue) > 3:
        #     del self.queue[0]
        # self.queue.append(self.unity_landmark)
        # self.unity_landmark = (sum(self.queue)) / len(self.queue)
        #===use MLS to optimize bones=======================
        # mls_smooth_numpy(map(float,list(range(len(self.queue))),list(self.queue),)
        # self.queue.append(self.unity_landmark)


    def merge(self):
        if self.pose_3d is None:
            self.img = None
            return

        visible_indices = np.where(self.pose_vis_depth.flatten() > 0.6)[0]  # 找到可见点的索引
        self.unity_landmark_depth = np.zeros((33, 3), dtype='float')
        for i in range(0, 33): # 左闭右开
            if i in visible_indices: # 将 self.pose_2d 中的可见点赋值到 self.unity_landmark
                self.unity_landmark_depth[i] = self.pose_2d[i]

        self.unity_landmark = np.zeros((69, 3), dtype = 'float')
        # 0 hips:     24+23 / 2
        self.unity_landmark[0] = (self.pose_2d[24] + self.pose_2d[23]) / 2
        # print(self.unity_landmark[0])

        # 7 Spine
        # 8 Chest
        # 9 UpperChest
        # 10 Neck
        upper_part_ = self.unity_landmark[0] - (self.pose_2d[12] + self.pose_2d[11]) / 2
        #print(upper_part_)
        up_part = upper_part_ / 1000
        self.unity_landmark[0] -= up_part * 145#167
        self.unity_landmark[7] = self.unity_landmark[0] - up_part * 219#164
        self.unity_landmark[8] = self.unity_landmark[7] - up_part * 213#173
        self.unity_landmark[9] = self.unity_landmark[8] - up_part * 198#156
        self.unity_landmark[10] = (self.pose_2d[12] + self.pose_2d[11]) / 2
        self.unity_landmark[10, 1] += (self.unity_landmark[10, 1] - self.unity_landmark[9, 1] ) / 1.67 #3.061 1.67

        # 14 LeftShoulder = 11;
        # 15 RightShoulder = 12;
        # 16 LeftLowerArm = 13;
        # 17 RightLowerArm = 14;
        # 18 LeftHand = 15;
        # 19 RightHand = 16;
        # 1 LeftUpperLeg 23
        # 2 RightUpperLeg 24
        # 3 LeftLowerLeg 25
        # 4 RightLowerLeg 26
        # 5 LeftFoot 27
        # 6 RightFoot 28
        # 20 LeftToe 31
        # 21 RightToe 32
        self.unity_landmark[1] = -self.pose_3d[23]
        self.unity_landmark[2] = -self.pose_3d[24]
        self.unity_landmark[3] = -self.pose_3d[25]
        self.unity_landmark[4] = -self.pose_3d[26]
        self.unity_landmark[5] = -self.pose_3d[27]
        self.unity_landmark[6] = -self.pose_3d[28]
        self.unity_landmark[14] = -self.pose_3d[11]
        self.unity_landmark[15] = -self.pose_3d[12]
        self.unity_landmark[16] =-self.pose_3d[13]
        self.unity_landmark[17] = -self.pose_3d[14]
        self.unity_landmark[18] = -self.pose_3d[15]
        self.unity_landmark[19] = -self.pose_3d[16]
        self.unity_landmark[20] = -self.pose_3d[31]
        self.unity_landmark[21] = -self.pose_3d[32]

        self.unity_landmark[12] = self.pose_2d[11]
        self.unity_landmark[13] = self.pose_2d[12]
        # self.unity_landmark[12] = -self.pose_3d[11]
        # self.unity_landmark[13] = -self.pose_3d[12]

        # 22 leftDetected, 23 rightDetected;默认000，如果检测到则为111
        if self.left_hands_2d is not None:
            self.unity_landmark[22] = [1.0, 1.0, 1.0]
            self.unity_landmark[24] = -self.left_hands_2d[0]
            self.unity_landmark[25] = -self.left_hands_2d[1]
            self.unity_landmark[26] = -self.left_hands_2d[2]
            self.unity_landmark[27] = -self.left_hands_2d[3]
            self.unity_landmark[28] = -self.left_hands_2d[4]
            self.unity_landmark[29] = -self.left_hands_2d[5]
            self.unity_landmark[30] = -self.left_hands_2d[6]
            self.unity_landmark[31] = -self.left_hands_2d[7]
            self.unity_landmark[32] = -self.left_hands_2d[8]
            self.unity_landmark[33] = -self.left_hands_2d[9]
            self.unity_landmark[34] = -self.left_hands_2d[10]
            self.unity_landmark[35] = -self.left_hands_2d[11]
            self.unity_landmark[36] = -self.left_hands_2d[12]
            self.unity_landmark[37] = -self.left_hands_2d[13]
            self.unity_landmark[38] = -self.left_hands_2d[14]
            self.unity_landmark[39] = -self.left_hands_2d[15]
            self.unity_landmark[40] = -self.left_hands_2d[16]
            self.unity_landmark[41] = -self.left_hands_2d[17]
            self.unity_landmark[42] = -self.left_hands_2d[18]
            self.unity_landmark[43] = -self.left_hands_2d[19]
            self.unity_landmark[44] = -self.left_hands_2d[20]

        if self.right_hands_2d is not None:
            self.unity_landmark[23] = [1.0, 1.0, 1.0]
            self.unity_landmark[45] = -self.right_hands_2d[0]
            self.unity_landmark[46] = -self.right_hands_2d[1]
            self.unity_landmark[47] = -self.right_hands_2d[2]
            self.unity_landmark[48] = -self.right_hands_2d[3]
            self.unity_landmark[49] = -self.right_hands_2d[4]
            self.unity_landmark[50] = -self.right_hands_2d[5]
            self.unity_landmark[51] = -self.right_hands_2d[6]
            self.unity_landmark[52] = -self.right_hands_2d[7]
            self.unity_landmark[53] = -self.right_hands_2d[8]
            self.unity_landmark[54] = -self.right_hands_2d[9]
            self.unity_landmark[55] = -self.right_hands_2d[10]
            self.unity_landmark[56] = -self.right_hands_2d[11]
            self.unity_landmark[57] = -self.right_hands_2d[12]
            self.unity_landmark[58] = -self.right_hands_2d[13]
            self.unity_landmark[59] = -self.right_hands_2d[14]
            self.unity_landmark[60] = -self.right_hands_2d[15]
            self.unity_landmark[61] = -self.right_hands_2d[16]
            self.unity_landmark[62] = -self.right_hands_2d[17]
            self.unity_landmark[63] = -self.right_hands_2d[18]
            self.unity_landmark[64] = -self.right_hands_2d[19]
            self.unity_landmark[65] = -self.right_hands_2d[20]


        if self.left_predict_result == "five":
            self.unity_landmark[67] = [0.0, 0.0, 0.0]
        else:
            self.unity_landmark[67] = [1.0, 1.0, 1.0]

        if self.right_predict_result == "five":
            self.unity_landmark[68] = [0.0, 0.0, 0.0]
        else:
            self.unity_landmark[68] = [1.0, 1.0, 1.0]

    def Track(self, image):
        if self.track_pose == True:
            self.Track_pose(image)
        if self.track_hands == True and self.pose_3d is not None:
            self.Track_hands_hands(image)
            # self.Track_hands_holistic(image)
        self.merge()
        self.optimizer()
