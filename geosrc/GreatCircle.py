"""
http://www.movable-type.co.uk/scripts/latlong.html
http://williams.best.vwh.net/ftp/avsig/avform.txt
"""

from math import radians, degrees, cos, sin, sqrt, atan2, asin, fabs

class Point(object):
    def __init__(self,x,y):
        self.x = x
        self.y = y

def distance_haversine(A, B, radius=6371000):
    dLat = radians(B.y-A.y)
    dLon = radians(B.x-A.x)
    lat1 = radians(A.y)
    lat2 = radians(B.y)
    a = sin(dLat/2) * sin(dLat/2) + sin(dLon/2) * sin(dLon/2) * cos(lat1) * cos(lat2)
    c = 2 * atan2(sqrt(a), sqrt(1-a))
    return c * radius

def bearing(A, B):
    dLon = radians(B.x-A.x)
    lat1 = radians(A.y)
    lat2 = radians(B.y)
    y = sin(dLon) * cos(lat2)
    x = cos(lat1)* sin(lat2) - sin(lat1) * cos(lat2)* cos(dLon)
    return atan2(y, x)

def bearing_degrees(A,B):
    return degrees(bearing(A,B))

def crosstrack(p,A,B, radius=6371000):
    dAp = distance_haversine(A, p, radius=1)
    brngAp = bearing(A,p)
    brngAB = bearing(A,B)
    dXt = asin(sin(dAp)*sin(brngAp-brngAB))
    return fabs(dXt) * radius

import unittest
class Test_gc(unittest.TestCase):
    def test_distance_haversine(self):
        d = distance_haversine(Point([-10.0001, 80.0001]), Point([7.935, 63.302]))
        self.assertAlmostEqual(d, 1939037.0, places=0)
        
    def test_bearing(self):
        b = bearing(Point(-10.0001, 80.0001), Point(7.935, 63.302))
        self.assertAlmostEqual(b, 2.661709,places=6)

    def test_bearing_degrees(self):
        b = bearing_degrees(Point(-10.0001, 80.0001), Point(7.935, 63.302))
        self.assertAlmostEqual(b, 152.504694,places=6)
        
    def test_crosstrack(self):
        self.fail("NOT IMPLEMENTED")

if __name__ == '__main__':
    unittest.main()
