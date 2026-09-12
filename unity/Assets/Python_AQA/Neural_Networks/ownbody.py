from keras.models import load_model
import numpy as np
import sys
import io
import Data_Load
# 设置标准输出为 UTF-8 编码
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', line_buffering=True)

try:
    # param1 = sys.argv[1]
    param1 = 'step_1'
    data_path = f"E:/Unity/wyf/try/Assets/Data/{param1}.txt"

    # 读取数据文件
    with open(data_path, 'r') as f:
        lines = f.readlines()

    X_train = Data_Load.load_data_from_txt(lines)

    # 处理数据
    train_x = np.concatenate((X_train[:, :2, :], X_train, X_train[:, -3:-1, :]), axis=1)

    train_x_2 = train_x[:, ::2, :]
    train_x_4 = train_x[:, ::4, :]
    train_x_8 = train_x[:, ::8, :]

    # 重新排序数据
    def reorder_data(x):
        X_trunk = x[:, :, 0:16]
        X_left_arm = x[:, :, 16:32]
        X_right_arm = x[:, :, 32:48]
        X_left_leg = x[:, :, 48:64]
        X_right_leg = x[:, :, 64:80]
        return np.concatenate((X_trunk, X_right_arm, X_left_arm, X_right_leg, X_left_leg), axis=-1)

    trainx = reorder_data(train_x)
    trainx_2 = reorder_data(train_x_2)
    trainx_4 = reorder_data(train_x_4)
    trainx_8 = reorder_data(train_x_8)

    # 加载模型
    model = load_model(r"E:\Unity\wyf\try\Assets\Python_AQA\Neural_Networks\my_model.h5")

    # 预测
    pred_train = model.predict([trainx, trainx_2, trainx_4, trainx_8], verbose=0)
    # 计算平均值
    mean_value = np.mean(pred_train)
    print(mean_value, flush=True)

    sys.exit(0)

except Exception as e:
    print(f"error: {e}", file=sys.stderr, flush=True)
    sys.stderr.flush()
    sys.exit(1)


