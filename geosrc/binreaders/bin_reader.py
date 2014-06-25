## Goal: reading (simple) binary raster files created with AsciiToBin.fsx

import glob,os, struct
import h5py
import numpy as np
import tables
import unittest, time

def read_values_naive(filename, indices):
    """ works but is very slow """
    # assume indices are sorted and unique
    values = []
    with open(filename, 'rb') as f:
        for index in indices:
            f.seek(index*4L, os.SEEK_SET)
            b = f.read(4)
            v = struct.unpack("@i", b)[0]
            if v <> -2147483648: # Nodata
                values.append(v)
            else:
                values.append(None)
    return values

def bin_to_hdf5(filename, ncells):
    """ convert our binary files to hdf5 files """
    newf = filename.replace('.sbg', '.hdf5')
    with h5py.File(newf, 'w') as outfile:
        outfile['raster'] = np.memmap(filename, dtype='int32', mode='r')
    return newf

def convert(v):
    if v <> -2147483648:
        return v
    else:
        return None

cache = {}
def read_values_hdf5(filename, indices):
    if filename not in cache:
        f = tables.open_file(filename)
        cache[filename] = f
    f = cache[filename]        
    dset = f.root.raster
    return map(convert, dset[indices])

def read_values_numpy_memmap(filename, indices):
    values_arr = np.memmap(filename, dtype='int32', mode='r')
    return map(convert, values_arr[indices])

# tests and related

def timefn(method):
    def timed(*args, **kw):
        ts = time.time()
        result = method(*args, **kw)
        te = time.time()
        print('%r %2.2f sec' % (method.__name__, te-ts))
        return result
    return timed

class Test_BinReader(unittest.TestCase):
    def load_test_indices(self):
        tests = { }
        with open('test_param.txt', 'r') as f:
            for _ in range(3):
                outerlen = int(f.readline())
                tests[outerlen] = []
                for __ in range(outerlen):
                    f.readline()
                    indices = map(long, f.readline().strip().split(";"))
                    #indices.sort()
                    tests[outerlen].append(indices)
        return tests

    @timefn
    def read_values_timed(self, reader, repeat, paths, indiceslist):
        r = []
        for i in xrange(repeat):
            for indices in indiceslist:
                for path in paths:
                    r = reader(path, indices)
        return r

    def test_small_marspec(self):
        testindices = self.load_test_indices()
        testindices.pop(1000)
        #testindices.pop(100)
        for outerlen,indiceslist in testindices.iteritems():
##            print('small marspec naive %i %i' % (outerlen, len(indiceslist[0])))
##            self.read_values_timed(read_values_naive, outerlen, [r'D:\temp\bathy_10m.sbg'], indiceslist)
            print('small marspec hdf5 %i %i' % (outerlen, len(indiceslist[0])))
            r = self.read_values_timed(read_values_hdf5, outerlen, [r'D:\temp\bathy_10m.hdf5'], indiceslist)
            print(r[0:10])
            print('small marspec numpy memmap %i %i' % (outerlen, len(indiceslist[0])))
            r = self.read_values_timed(read_values_numpy_memmap, outerlen, [r'D:\temp\bathy_10m.sbg'], indiceslist)
            print(r[0:10])
            
##    def test_all_marspec(self):
##        paths = glob.glob(r'D:\a\data\marspec\MARSPEC_10m\ascii\*.asc')
##        paths = [os.path.join(r'D:\temp', os.path.split(p)[1].replace('.asc','.sbg')) for p in paths]
##        testindices = self.load_test_indices()
##        for outerlen,indiceslist in testindices.iteritems():
##            print('%i %i numpy memmap all marspec 10m' % (outerlen, len(indiceslist[0])))
##            r = self.read_values_timed(read_values_numpy_memmap, outerlen, paths, indiceslist)
##            print('%i %i hdf5 all marspec 10m' % (outerlen, len(indiceslist[0])))
##            r = self.read_values_timed(read_values_hdf5, outerlen, [p.replace('.sbg','.hdf5') for p in paths], indiceslist)
##         
if __name__ == '__main__':
    unittest.main()
    #bin_to_hdf5(r'D:\temp\bathy_10m.sbg', 2160*1080)
    
    
