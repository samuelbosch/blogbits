function degToRad(d)
    d*0.0174532925199433
end

function project(centerlon, centerlat, lon, lat)
  t = degToRad(lat)
  l = degToRad(lon)
  t1 = degToRad(centerlat)
  l0 = degToRad(centerlon)
  c = acos(sin(t1) * sin(t) + cos(t1) * cos(t) * cos(l-l0))
  k = c / sin(c)
  x = k * cos(t) * sin(l-l0)
  y = k * (cos(t1) * sin(t) - sin(t1) * cos(t) * cos(l-l0))
  x, y
end