import os
import struct
import glob
import time


def read_values(filename, indices):
    # indices are sorted and unique
    values = []
    with open(filename, 'rb') as f:
        for index in indices:
            f.seek(index*4L, os.SEEK_SET)
            b = f.read(4)
            v = struct.unpack("@i", b)[0]
            if v == -2147483648:
                v = None
            values.append(v)
    return values


def getindices(n):
    return [10000+(i*3) for i in range(n)]


def timefn(method):
    def timed(*args, **kw):
        ts = time.time()
        result = method(*args, **kw)
        te = time.time()
        print('%s%s %2.2f sec' % (method.__name__, str(list(args)), te-ts))
        return result
    return timed


@timefn
def allmarspec(outer, inner):
    paths = glob.glob(r'D:\temp\sbg_10m\*.sbg')
    indices = getindices(inner)
    r = []
    for i in range(outer):
        r = [read_values(os.path.join(r'D:\temp\sbg_10m', path), indices) for path in paths]
    return r

if __name__ == '__main__':
    allmarspec(1, 10)  # 0.01 sec
    #allmarspec(10,10000)  # <26 sec
    allmarspec(10000, 10)  # <26 sec
