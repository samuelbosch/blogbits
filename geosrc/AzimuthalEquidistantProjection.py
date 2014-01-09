from math import cos, sin

class Point(object):
    def __init__(self,x,y):
        self.x = x
        self.y = y       

class AzimuthalEquidistantProjection(object):
    """ 
        http://mathworld.wolfram.com/AzimuthalEquidistantProjection.html
        http://www.radicalcartography.net/?projectionref
    """
    DEGTORAD = 0.01745329251
    def __init__(self, center):
        self.center = center
        self.t1 = center.y * self.DEGTORAD ## latitude center of projection
        self.l0 = center.x * self.DEGTORAD ## longitude center of projection
        self.cost1 = cos(self.t1)

    def project(self, point):
        t = self.DEGTORAD * point.y
        l = self.DEGTORAD * point.x
        costcosll0 = cos(t) * cos(l-self.l0)
        sint = sin(t)
        sint1 = sin(self.t1)
        c = math.acos ((sint1) * (sint) + (self.cost1) * costcosll0)
        k = c / sin(c)
        x = k * cos(t) * sin(l-self.l0)
        y = k * self.cost1 * sint - sint1 * costcosll0
        return Point([x, y])

import unittest
class Test_AzimuthalEquidistantProjection(unittest.TestCase):
    def test_project(self):
        p = AzimuthalEquidistantProjection(Point(0.0,0.0))
        r = p.project(Point(1.0,1.0))
        self.assertAlmostEqual(0.01745152022, r.x)
        self.assertAlmostEqual(0.01745417858, r.y)

        p = AzimuthalEquidistantProjection(Point(1.0,2.0))
        r = p.project(Point(3.0,4.0))
        self.assertAlmostEqual(0.03482860733, r.x)
        self.assertAlmostEqual(0.03494898734, r.y)

        p = AzimuthalEquidistantProjection(Point(-10.0001, 80.0001))
        r = p.project(Point(7.935, 63.302))
        self.assertAlmostEqual(0.1405127567, r.x)
        self.assertAlmostEqual(-0.263406547, r.y)


if __name__ == '__main__':
    unittest.main()
