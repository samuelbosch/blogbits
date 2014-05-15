""" some references
http://www.movable-type.co.uk/scripts/latlong.html
http://williams.best.vwh.net/ftp/avsig/avform.txt
"""

from math import radians, degrees, cos, sin, sqrt, atan2, asin, fabs, pi

class Point(object):
    def __init__(self,x,y):
        self.x = x
        self.y = y

def distance_haversine(A, B, radius=6371000):
    """ note that the default distance is in meters """
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

def midpoint(A,B):
    ## little shortcut
    if A.x == B.x: return Point(A.x, (A.y+B.y)/2)
    if A.y == B.y: return Point((A.x+B.x)/2, A.y)
    
    lon1, lat1 = radians(A.x), radians(A.y)
    lon2, lat2 = radians(B.x), radians(B.y)
    dLon = lon2-lon1

    Bx = cos(lat2) * cos(dLon)
    By = cos(lat2) * sin(dLon)
    lat3 = atan2(sin(lat1)+sin(lat2), sqrt((cos(lat1)+Bx)*(cos(lat1)+Bx) + By*By))
    lon3 = lon1 + atan2(By, cos(lat1) + Bx)
    return Point(degrees(lon3),degrees(lat3))

def crosstrack_error(p,A,B, radius=6371000):
    """ distance (in meters) from a point to the closest point along a track
        http://williams.best.vwh.net/avform.htm#XTE """
    dAp = distance_haversine(A, p, radius=1)
    brngAp = bearing(A,p)
    brngAB = bearing(A,B)
    dXt = asin(sin(dAp)*sin(brngAp-brngAB))
    return fabs(dXt) * radius

def point_line_distance(p, A,B, radius=6371000, tolerance=0.1): # tolerance and radius in meters
    """ recursive function that halves the search space until result is within tolerance
        disadvantages:
        1) the rounding errors in midpoint do add up
        2) not very fast """
    def rec_point_line_distance(p,A,B,dA,dB):
        C = midpoint(A,B)
        dC = distance_haversine(p,C)
        if fabs(dC-dA) < tolerance or fabs(dC-dB) < tolerance:
            return dC
        elif dA < dB:
            return point_line_distance(p,A,C,dA, dC)
        else:
            return point_line_distance(p,C,B,dC, dB)
    dA = distance_haversine(p,A)
    dB = distance_haversine(p,B)
    return rec_point_line_distance(p,A,B,dA,dB)

def destination_point(p, distanceR, bearing):
    x,y = radians(p.x),radians(p.y)
    y2 = asin(sin(y)*cos(distanceR) + cos(y)*sin(distanceR)*cos(bearing))
    x2 = x + atan2(sin(bearing)*sin(distanceR)*cos(y), 
                   cos(distanceR)-sin(y)*sin(y2))
    x2 = (x2+3*pi) % (2*pi) - pi;  ## normalise to -180..+180
    return Point(degrees(x2),degrees(y2))

def get_centroid(points):
    """ 
    http://www.geomidpoint.com/example.html 
    http://gis.stackexchange.com/questions/6025/find-the-centroid-of-a-cluster-of-points
    """
    sum_x,sum_y,sum_z = 0,0,0
    for p in points:
        lat = radians(p.y)
        lon = radians(p.x)
        ## convert lat lon to cartesian coordinates
        sum_x = sum_x + cos(lat) * cos(lon)
        sum_y = sum_y + cos(lat) * sin(lon)
        sum_z = sum_z + sin(lat)
    avg_x = sum_x / float(len(points))
    avg_y = sum_y / float(len(points))
    avg_z = sum_z / float(len(points))
    center_lon = atan2(avg_y,avg_x)
    hyp = sqrt(avg_x*avg_x + avg_y*avg_y) 
    center_lat = atan2(avg_z, hyp)
    return Point(degrees(center_lon), degrees(center_lat))

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

    def test_midpoint(self):
        m = midpoint(Point(-20.0,5.0), Point(-10.0,5.0))
        self.assertAlmostEqual(-15.0, m.x, places=6)
        m = midpoint(Point(7.0,5.0), Point(7.0,15.0))
        self.assertAlmostEqual(10.0, m.y, places=6)
        
    def test_crosstrack_error(self):
        p,A,B = Point(0.0, 1.0), Point(1.0, 1.0), Point(2.0, 1.0)
        d = crosstrack_error(p,A,B)
        self.assertAlmostEqual(d, distance_haversine(p,A), places=0)
        ## Apparently crosstrack_error returns incorrect results in some very specific cases (when p is completly off track)
        
    def test_point_line_distance(self):
        
        p,A,B = Point(0.0, 1.0), Point(1.0, 1.0), Point(2.0, 1.0)
        d = point_line_distance(p,A,B)
        self.assertAlmostEqual(d, distance_haversine(p,A), places=0)

        p,A,B = Point(2.0, 1.2), Point(1.0, 1.0), Point(3.0, 1.0)
        d = point_line_distance(p,A,B)
        self.assertAlmostEqual(d, distance_haversine(p,Point(2.0,1.0)), places=0)

        p,A,B = Point(0.0, 90.0), Point(-179.0, -21.0), Point(179.5, -22.0)
        d = point_line_distance(p,A,B)
        self.assertAlmostEqual(d, distance_haversine(p,A), places=0)

        p,A,B = Point(0.0, 89.0), Point(-179.0, -22.0), Point(179.5, -22.0)
        d = point_line_distance(p,A,B)
        self.assertAlmostEqual(d, distance_haversine(p,Point(0, -22)), places=0)
        
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
