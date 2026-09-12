import sys

def add(a, b):
    return float(a) + float(b)


if __name__ == '__main__':
    param1 = sys.argv[1]
    param2 = sys.argv[2]
    print('Hello World')
    print(add(param1,param2))