"""
The file loads the data for full body skeletons

"""

import csv
import numpy as np

def load_data():
    f = open('Data_KIMORE_e5/Train_X.csv')
    csv_f = csv.reader(f)
    Train_X = list(csv_f)

    # Convert the input sequences into numpy arrays
    train_input1 = np.asarray(Train_X, dtype = float)
    n_dim = 88
    train_input = np.zeros((204,100,n_dim))
    for i in range(len(train_input1)//100):
          train_input[i,:,:] = train_input1[100*i:100*(i+1),:]
    # 从 20400 x 88 的原始数据中提取：每 100 个样本作为一个序列。
    # 将数据重塑为 204 x 100 x 88：每个序列有 100 个时间步，每个时间步有 88 个特征。
    
    f = open('Data_KIMORE_e5/Train_Y.csv')
    csv_f = csv.reader(f)
    Train_Y = list(csv_f)
    
    # Convert the input labels into numpy arrays
    train_label = np.transpose(np.asarray(Train_Y[0], dtype = float))
    # 标签处理：转换为列向量，标签维度是 (204,)
    
    return train_input, train_label



def load_data_from_txt(lines):

    # Process each line and convert to a list of floats
    data = []
    for i, line in enumerate(lines):
        # Select the 1st, 3rd, 5th lines, etc. (even index in zero-based counting)
        if i % 2 == 0:
            # Remove the first two characters and convert the remaining part to float
            line = line[2:]  # Remove first two characters
            data.append(list(map(float, line.strip().split()[:66])))  # Split line into numbers and convert to float
    # Convert the list of lists into a numpy array
    data_array = np.array(data)
    # print('data_array', data_array.shape)

    # data_array.shape => (107, 66),前22个点，要变成16*5=80个点

    # 设定映射顺序
    mapping_order = [0, 7, 8, 10, 12, 14, 16, 18, 13, 15, 17, 19, 1, 3, 5, 20, 2, 4, 6, 21]

    # 需要在坐标后添加0的索引
    add_zero_indexes = [10, 18, 19, 20, 21]

    # 创建一个新的数组来存储重新排列后的数据
    reshaped_data = []

    for line in data_array:
        new_line = []
        for index in mapping_order:
            # 每个坐标有三个数据 (x, y, z)，所以每次取出3个数据
            new_line.extend(line[3 * index: 3 * (index + 1)])  # 获取对应的坐标

            # 如果当前索引在需要添加零的位置，添加4个0
            if index in add_zero_indexes:
                new_line.extend([0] * 4)  # 添加4个0

        reshaped_data.append(new_line)

    # 将列表转为numpy数组
    reshaped_data_array = np.array(reshaped_data)

    # 查看结果的形状
    # print('reshaped_data_array shape:', reshaped_data_array.shape)

    # Define n_dim (which is 69 based on the description) and other parameters
    n_dim = 80
    sequence_length = 100  # You can change this based on your data
    total_samples = len(reshaped_data_array) // sequence_length  # Calculate number of samples

    # Initialize a numpy array to hold the reshaped data
    reshaped_data = np.zeros((total_samples, sequence_length, n_dim))

    # Fill the reshaped data array with the sequences
    for i in range(total_samples):
        reshaped_data[i, :, :] = reshaped_data_array[sequence_length * i: sequence_length * (i + 1), :]

    return reshaped_data

