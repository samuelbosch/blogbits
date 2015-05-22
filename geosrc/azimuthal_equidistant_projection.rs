
pub fn project(centerlon: f64, centerlat: f64, lon: f64, lat: f64) -> (f64,f64) {
  let t = lat.to_radians();
  let l = lon.to_radians();
  let t1 = centerlat.to_radians();
  let l0 = centerlon.to_radians();
  let c = (t1.sin() * t.sin() + t1.cos() * t.cos() * (l-l0).cos()).acos();
  let k = c / c.sin();
  let x = k * t.cos() * (l-l0).sin();
  let y = k * (t1.cos() * t.sin() - t1.sin() * t.cos() * (l-l0).cos());
  return(x,y);
}