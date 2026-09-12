from yacs.config import CfgNode as CN
import numpy as np
_C = CN()

#==========unity==================
_C.UNITY=CN()
_C.UNITY.IS_CONNECT = True
_C.UNITY.PORT = 9600
_C.UNITY.ADDRESS = "127.0.0.1"
#===========mediapipe====================
_C.MEDPIPE=CN()
# cap的时候设置为False
_C.MEDPIPE.SHOW = False
# MODE: video/cap
# the mode of video,the address of video
# _C.MEDPIPE.MODE = "video"
# _C.MEDPIPE.VIDEO_PATH = "./videos/1.mp4"
_C.MEDPIPE.MODE = "cap"

# the fov of camera
_C.MEDPIPE.CAMERA_FOV = 60
_C.MEDPIPE.TRACK_HANDS = True
_C.MEDPIPE.SMOOTH_RANGE = 5
_C.MEDPIPE.BARYCENTER_SMOOTH_RANGE= 20

#=========IK SOLVER=============
_C.IK_SOLVER = CN()
_C.IK_SOLVER.SMOOTH_RANGE = 15
#===========camera============
_C.CAMERA=CN()
_C.CAMERA.RATE=30
_C.CAMERA.WIDTH=640# 30帧：1280 × 720 ； 90帧 640 × 480
_C.CAMERA.HEIGHT=480
#===========post-processing============
_C.PROCESS=CN()
_C.PROCESS.SAMTIMES=1 # 0.5, 降低 x-y 分辨率 2 倍, downsample
# _C.PROCESS.FILSIZE=3 # 3, 3x3 中值滤波, median filtering
# _C.PROCESS.IRRFILALPHA=0.4 # IIR过滤器：y[n]=α⋅x[n]+(1−α)⋅y[n−1]

def get_cfg_defaults():
    return _C.clone()