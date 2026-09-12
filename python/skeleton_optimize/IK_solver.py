import torch
from typing import List, Tuple
from skeleton_optimize.utils3d import mls_smooth

class SkeletonIKSolver:
    def __init__(self, smooth_range: float = 0.3):
        # 平滑参数
        self.smooth_range = smooth_range
        self.location_history: List[Tuple[torch.Tensor, float]] = []

    def fit_location(self, kpts: torch.Tensor, frame_t: float):
        """
        添加新的位置数据到历史记录中，适用于逐帧更新位置。
        """
        # location = kpts.mean(dim=0)
        location = kpts
        self.location_history.append((location, frame_t))
        # print("location",location)

    def get_smoothed_location(self, query_t: float) -> torch.Tensor:
        """
        获取在指定时间点的平滑位置。
        """
        input_location, input_t = zip(
            *((e, t) for e, t in self.location_history if abs(t - query_t) < self.smooth_range)
        )
        if len(input_t) <= 2:
            location_smoothed = input_location[-1]
        else:
            location_smoothed = mls_smooth(input_t, input_location, query_t, self.smooth_range)
        return location_smoothed
