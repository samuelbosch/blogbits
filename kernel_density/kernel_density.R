library("KernSmooth")
library("raster")

input <- "Alaria_esculenta.csv"
output <- "Alaria_esculenta_density.asc"

# get the coordinates
records <- read.csv(input)
coordinates <- records[,2:3]

# compute the 2D binned kernel density estimate
est <- bkde2D(coordinates, 
              bandwidth=c(3,3), 
              gridsize=c(4320,2160),
              range.x=list(c(-180,180),c(-90,90)))
est$fhat[est$fhat<0.00001] <- 0 ## ignore very small values

# create raster
est.raster = raster(list(x=est$x1,y=est$x2,z=est$fhat))
projection(est.raster) <- CRS("+init=epsg:4326")
xmin(est.raster) <- -180
xmax(est.raster) <- 180
ymin(est.raster) <- -90
ymax(est.raster) <- 90
# visually inspect the raster output
plot(est.raster)
# write an ascii file
writeRaster(est.raster,output,"ascii")
