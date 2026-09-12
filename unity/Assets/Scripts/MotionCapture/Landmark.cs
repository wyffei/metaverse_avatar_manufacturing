using System;

namespace BodyTracking  // 为 Landmark 和 LandmarkList 类添加命名空间
{
    [System.Serializable]
    public class Landmark
    {
        public float x;
        public float y;
        public float z;
        public override bool Equals(object obj)
        {
            if (obj is Landmark other)
            {
                return Math.Abs(this.x - other.x) < 0.0001f &&
                       Math.Abs(this.y - other.y) < 0.0001f &&
                       Math.Abs(this.z - other.z) < 0.0001f;
            }
            return false;
        }

        // 重写 ToString() 方法来返回 Landmark 的坐标值
        public override string ToString()
        {
            return $"x: {x}, y: {y}, z: {z}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public Landmark(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [System.Serializable]
    public class LandmarkList
    {
        public Landmark[] landmarks;

        public LandmarkList(Landmark[] landmarks)
        {
            this.landmarks = landmarks;
        }
    }



}
