library("splancs")
library("raster")
library("sp")

input <- "Alaria_esculenta.csv"
output <- "Alaria_esculenta_density.asc"

ncols <- 4320
nrows <- 2160
records <- read.csv(input)
coordinates <- records[,2:3]
poly <- matrix(c(-180.0,-180.0,180.0,180.0,-180.0, -90.0,90.0,90.0,-90.0,-90.0) ,ncol=2)
grid <- GridTopology(c(-180,-90), c(360.0/ncols, 180.0/nrows), c(ncols,nrows))
crds <- SpatialPoints(coordinates, proj4string=CRS("+init=epsg:4326"))
est <- spkernel2d(crds, poly, h0=3, grid)

est.raster <- raster(SpatialGridDataFrame(grid, data=data.frame(est=est)))

projection(est.raster)=CRS("+init=epsg:4326")
xmin(est.raster)=-180
xmax(est.raster)=180
ymin(est.raster)=-90
ymax(est.raster)=90
# write an ascii file
writeRaster(est.raster,output,"ascii",overwrite=TRUE,NAflag=-9999)