read.values <- function(filename, indices) {
  conn <- file(filename, "rb")
  read.value <- function(index) {
    seek(conn, where=index*4)
    readBin(conn, integer(), size = 4, n = 1, endian = "little")
  }
  r <- sapply(indices,read.value)
  close(conn)
  r
}

get.indices <- function(n) {
  seq(10000,10000+(n*3)-1, by=3)
}

smallmarspec <- function(outer, inner) {
  result <- NULL
  for (i in 1:outer) {
    r <- read.values("D:\\temp\\bathy_10m.sbg", get.indices(n))
  }
}

allmarspec <- function(outer, inner) {
  paths <- c('D:\\temp\\bathy_10m.sbg', 'D:\\temp\\bathy_10m_plus_300.sbg', 'D:\\temp\\biogeo01_aspect_EW_10m.sbg', 'D:\\temp\\biogeo02_aspect_NS_10m.sbg', 'D:\\temp\\biogeo03_plan_curvature_10m.sbg', 'D:\\temp\\biogeo04_profile_curvature_10m.sbg', 'D:\\temp\\biogeo05_dist_shore_10m.sbg', 'D:\\temp\\biogeo06_bathy_slope_10m.sbg', 'D:\\temp\\biogeo07_concavity_10m.sbg', 'D:\\temp\\biogeo08_sss_mean_10m.sbg', 'D:\\temp\\biogeo09_sss_min_10m.sbg', 'D:\\temp\\biogeo10_sss_max_10m.sbg', 'D:\\temp\\biogeo11_sss_range_10m.sbg', 'D:\\temp\\biogeo12_sss_variance_10m.sbg', 'D:\\temp\\biogeo13_sst_mean_10m.sbg', 'D:\\temp\\biogeo14_sst_min_10m.sbg', 'D:\\temp\\biogeo15_sst_max_10m.sbg', 'D:\\temp\\biogeo16_sst_range_10m.sbg', 'D:\\temp\\biogeo17_sst_variance_10m.sbg', 'D:\\temp\\sss01_10m.sbg', 'D:\\temp\\sss02_10m.sbg', 'D:\\temp\\sss03_10m.sbg', 'D:\\temp\\sss04_10m.sbg', 'D:\\temp\\sss05_10m.sbg', 'D:\\temp\\sss06_10m.sbg', 'D:\\temp\\sss07_10m.sbg', 'D:\\temp\\sss08_10m.sbg', 'D:\\temp\\sss09_10m.sbg', 'D:\\temp\\sss10_10m.sbg', 'D:\\temp\\sss11_10m.sbg', 'D:\\temp\\sss12_10m.sbg', 'D:\\temp\\sst01_10m.sbg', 'D:\\temp\\sst02_10m.sbg', 'D:\\temp\\sst03_10m.sbg', 'D:\\temp\\sst04_10m.sbg', 'D:\\temp\\sst05_10m.sbg', 'D:\\temp\\sst06_10m.sbg', 'D:\\temp\\sst07_10m.sbg', 'D:\\temp\\sst08_10m.sbg', 'D:\\temp\\sst09_10m.sbg')
  indices <- get.indices(inner)
  reader <- function(filename) { read.values(filename, indices) }
  r <- NULL
  for (i in 1:outer) {
    r <- sapply(paths, reader)
  }
  r
}

system.time(allmarspec(10,10)) #0.15ms
system.time(allmarspec(100,100)) #11s
#system.time(allmarspec(1000,100)) #115s
