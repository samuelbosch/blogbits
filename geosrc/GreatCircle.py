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

def destination_point(p, distanceR, bearing):
    x,y = math.radians(p.x),math.radians(p.y)
    y2 = math.asin(math.sin(y)*math.cos(distanceR) + 
                   math.cos(y)*math.sin(distanceR)*math.cos(bearing))
    x2 = x + math.atan2(math.sin(bearing)*math.sin(distanceR)*math.cos(y), 
                               math.cos(distanceR)-math.sin(y)*math.sin(y2));
    x2 = (lon2+3*math.PI) % (2*math.PI) - math.PI;  ## normalise to -180..+180
    return math.degrees(x2),math.degrees(y2)

def get_centroid(points):
    """ 
        http://www.geomidpoint.com/example.html 
        http://gis.stackexchange.com/questions/6025/find-the-centroid-of-a-cluster-of-points
    """
    sum_x,sum_y,sum_z = 0,0,0
    for p in points:
        lat = math.radians(p.y)
        lon = math.radians(p.x)
        ## convert lat lon to cartesian coordinates
        sum_x = sum_x + math.cos(lat) * math.cos(lon)
        sum_y = sum_y + math.cos(lat) * math.sin(lon)
        sum_z = sum_z + math.sin(lat)
    avg_x = sum_x / float(len(points))
    avg_y = sum_y / float(len(points))
    avg_z = sum_z / float(len(points))
    center_lon = math.atan2(avg_y,avg_x)
    hyp = math.sqrt(avg_x*avg_x + avg_y*avg_y) 
    center_lat = math.atan2(avg_z, hyp)
    return Point([math.degrees(center_lon), math.degrees(center_lat)])

import unittest
class Test_gc(unittest.TestCase):
    def test_distance_haversine(self):
        d = distance_haversine(Point(-10.0001, 80.0001), Point(7.935, 63.302))
        self.assertAlmostEqual(d, 1939037.0, places=0)
        
    def test_bearing(self):
        b = bearing(Point(-10.0001, 80.0001), Point(7.935, 63.302))
        self.assertAlmostEqual(b, 2.661709,places=6)

    def test_bearing_degrees(self):
        b = bearing_degrees(Point(-10.0001, 80.0001), Point(7.935, 63.302))
        self.assertAlmostEqual(b, 152.504694,places=6)
        
    def test_crosstrack(self):
        self.fail("NOT IMPLEMENTED")

    def test_destination_point(self):
        self.fail("NOT IMPLEMENTED")

    def test_get_centroid(self):
        ## check crosses the north pole
        center = get_centroid([Point(0.0, 80.0), Point(180.0,80.0)])
        self.assertEqual(90.0, center.y)
        ## check multiple points
        center = get_centroid([Point(0.0, 0.0), Point(10.0,0.0), Point(5.0,10.0)])
        self.assertAlmostEqual(5.0, center.x)
        ## even more points
        center = get_centroid([Point(0.0, 0.0), Point(10.0,0.0),Point(10.0,30.0), Point(10.0,10.0), Point(20.0, 0.0)])
        self.assertAlmostEqual(10.0, center.x)
        ## not lat lon average
        center = get_centroid([Point(0.0, 30.0), Point(10.0,30.0),Point(10.0,60.0), Point(0.0,60.0)])
        self.assertNotAlmostEqual(45.0, center.y)
        self.assertAlmostEqual(5.0, center.x)
        ## crosses date line
        center = get_centroid([Point(170.0, 30.0), Point(-160.0,30.0),Point(170.0,60.0), Point(-160.0,60.0)])
        self.assertNotAlmostEqual(45.0, center.y)
        self.assertAlmostEqual(-175.0, center.x)

if __name__ == '__main__':
    unittest.main()
