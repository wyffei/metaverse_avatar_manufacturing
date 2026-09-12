from typing import List

import numpy as np
import torch

# _axis_angle_rotation
# 根据输入的轴（X、Y、Z）和角度，生成相应的 4x4 旋转矩阵。
# euler_angle_to_matrix
# 将欧拉角（euler_angles）按照指定顺序（convention）转换为旋转矩阵（Rotation Matrix）。
# 用于 3D 空间中的旋转操作，比如骨骼动画或相机视角的计算。
def _axis_angle_rotation(axis: str, angle: torch.Tensor) -> torch.Tensor:
    """
    Return the rotation matrices for one of the rotations about an axis
    of which Euler angles describe, for each value of the angle given.

    Args:
        axis: Axis label "X" or "Y or "Z".
        angle: any shape tensor of Euler angles in radians

    Returns:
        Rotation matrices as tensor of shape (..., 3, 3).
    """

    cos = torch.cos(angle)
    sin = torch.sin(angle)
    one = torch.ones_like(angle)
    zero = torch.zeros_like(angle)

    if axis == "X":
        R_flat = (
            one, zero, zero, zero,
            zero, cos, -sin, zero,
            zero, sin, cos, zero,
            zero, zero, zero, one
        )
    elif axis == "Y":
        R_flat = (
            cos, zero, sin, zero,
            zero, one, zero, zero,
            -sin, zero, cos, zero,
            zero, zero, zero, one
        )
    elif axis == "Z":
        R_flat = (
            cos, -sin, zero, zero, 
            sin, cos, zero, zero,
            zero, zero, one, zero,
            zero, zero, zero, one
        )
    else:
        raise ValueError("letter must be either X, Y or Z.")

    return torch.stack(R_flat, -1).reshape(angle.shape + (4, 4))

def euler_angle_to_matrix(euler_angles: torch.Tensor, convention: str) -> torch.Tensor:
    """
    Code MODIFIED from pytorch3d
    Convert rotations given as Euler angles in radians to rotation matrices.

    Args:
        euler_angles: Euler angles in radians as tensor of shape (..., 3), XYZ
        convention: permutation of "X", "Y" or "Z", representing the order of Euler rotations to apply.

    Returns:
        Rotation matrices as tensor of shape (..., 3, 3).
    """
    if euler_angles.dim() == 0 or euler_angles.shape[-1] != 3:
        raise ValueError("Invalid input euler angles.")
    if len(convention) != 3:
        raise ValueError("Convention must have 3 letters.")
    if convention[1] in (convention[0], convention[2]):
        raise ValueError(f"Invalid convention {convention}.")
    for letter in convention:
        if letter not in ("X", "Y", "Z"):
            raise ValueError(f"Invalid letter {letter} in convention string.")
    matrices = [
        _axis_angle_rotation(c, euler_angles[..., 'XYZ'.index(c)])
        for c in convention
    ]
    # return functools.reduce(torch.matmul, matrices)
    return matrices[2] @ matrices[1] @ matrices[0]

# 相机内参矩阵计算
def intrinsic_from_fov(fov: float, width: int, height: int) -> np.ndarray:
    normed_int =  np.array([
        [0.5 / (np.tan(fov / 2) * (width / max(width, height))), 0., 0.5],
        [0., 0.5 / (np.tan(fov / 2) * (height / max(width, height))), 0.5],
        [0., 0., 1.],
    ], dtype=np.float32)
    return normed_int * np.array([width, height, 1], dtype=np.float32).reshape(3, 1)

# 移动最小二乘法平滑
#moving_least_square
# 对输入的点（x, y）和权重（w）进行移动最小二乘拟合，返回拟合曲线的多项式系数。
# 利用权重控制拟合时对不同数据点的影响程度。
# mls_smooth
# 对输入序列（input_t 和 input_y）进行平滑，返回在指定时间点（query_t）的拟合值。平滑范围由 smooth_range 决定。
def moving_least_square(x: torch.Tensor, y: torch.Tensor, w: torch.Tensor):
    # 1-D MLS: x: (..., N), y: (..., N), w: (..., N)
    p = torch.stack([torch.ones_like(x), x], dim=-2)             # (..., 2, N)
    M = p @ (w[..., :, None] * p.transpose(-2, -1))
    M = M.float()  # 将 M 转换为 float32
    a = torch.linalg.solve(M, (p @ (w * y)[..., :, None]))
    a = a.squeeze(-1)
    return a
def mls_smooth(input_t: List[float], input_y: List[np.ndarray], query_t: float, smooth_range: float):
    # 1-D MLS: input_t: (N), input_y: (..., N), query_t: scalar
    if len(input_y) == 1:
        return input_y[0]
    input_t = torch.tensor(input_t) - query_t
    input_y = torch.stack(input_y, axis=-1)
    broadcaster = (None,)*(len(input_y.shape) - 1)
    w = torch.maximum(smooth_range - torch.abs(input_t), torch.tensor(0))
    input_t = input_t.float()  # 将 input_t 转换为 float32
    input_y = input_y.float()  # 将 input_y 转换为 float32
    w = w.float()  # 将 w 转换为 float32
    coef = moving_least_square(input_t[broadcaster], input_y, w[broadcaster])
    return coef[..., 0]

#moving_least_square_numpy 和 mls_smooth_numpy
# 功能与 Torch 版本一致，但适合在 NumPy 环境中使用。
def moving_least_square_numpy(x: np.ndarray, y: np.ndarray, w: np.ndarray):
    # 1-D MLS: x: (..., N), y: (..., N), w: (..., N)
    p = np.stack([np.ones_like(x), x], axis=-2)             # (..., 2, N)
    M = p @ (w[..., :, None] * p.swapaxes(-2, -1))
    a = np.linalg.solve(M, (p @ (w * y)[..., :, None]))
    a = a.squeeze(-1)
    return a
def mls_smooth_numpy(input_t: List[float], input_y: List[np.ndarray], query_t: float, smooth_range: float):
    # 1-D MLS: input_t: (N), input_y: (..., N), query_t: scalar
    if len(input_y) == 1:
        return input_y[0]
    input_t = np.array(input_t) - query_t
    input_y = np.stack(input_y, axis=-1)
    broadcaster = (None,)*(len(input_y.shape) - 1)
    w = np.maximum(smooth_range - np.abs(input_t), 0)
    coef = moving_least_square_numpy(input_t[broadcaster], input_y, w[broadcaster])
    return coef[..., 0]
