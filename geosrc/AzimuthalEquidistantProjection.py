from math import cos, sin, acos, radians

class Point(object):
    def __init__(self,x,y):
        self.x = x
        self.y = y       

class AzimuthalEquidistantProjection(object):
    """ 
        http://mathworld.wolfram.com/AzimuthalEquidistantProjection.html
        http://www.radicalcartography.net/?projectionref
    """
    def __init__(self, center):
        self.center = center
        self.t1 = radians(center.y) ## latitude center of projection
        self.l0 = radians(center.x) ## longitude center of projection
        self.cost1 = cos(self.t1)
        self.sint1 = sin(self.t1)
        
    def project(self, point):
        t = radians(point.y)
        l = radians(point.x)
        costcosll0 = cos(t) * cos(l-self.l0)
        sint = sin(t)
        
        c = acos ((self.sint1) * (sint) + (self.cost1) * costcosll0)
        k = c / sin(c)
        x = k * cos(t) * sin(l-self.l0)
        y = k * (self.cost1 * sint - self.sint1 * costcosll0)
        return Point(x, y)

import unittest
class Test_AzimuthalEquidistantProjection(unittest.TestCase):
    def test_project(self):
        # checked with R
        # coordinates(spTransform(SpatialPoints(matrix(c(1,1), nrow=1), proj4string=wgs84), CRS("+proj=aeqd  +R=1 +lat_0=0) +lon_0=0"))
        p = AzimuthalEquidistantProjection(Point(0.0,0.0))
        r = p.project(Point(1.0,1.0))
        self.assertAlmostEqual(0.01745152, r.x)
        self.assertAlmostEqual(0.01745418, r.y)

        p = AzimuthalEquidistantProjection(Point(1.0,2.0))
        r = p.project(Point(3.0,4.0))
        self.assertAlmostEqual(0.03482861, r.x)
        self.assertAlmostEqual(0.03493487, r.y)
        
        p = AzimuthalEquidistantProjection(Point(-10.0001, 80.0001))
        r = p.project(Point(7.935, 63.302))
        self.assertAlmostEqual(0.1405128, r.x)
        self.assertAlmostEqual(-0.2699765, r.y)

if __name__ == '__main__':
    unittest.main()
