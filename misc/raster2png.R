# Version 1: using the raster::plot function and png function

if(!requireNamespace("sdmpredictors")) {
  install.packages("sdmpredictors")
}
library(raster)
x <- sdmpredictors::load_layers("BO_bathymean", equalarea = TRUE)
png("bathymetry_plot1.png", width=ncol(x), height=nrow(x))
col <- rev(c("#f7fbff", "#deebf7", "#c6dbef", "#9ecae1", "#6baed6", "#4292c6","#2171b5","#08519c", rep("#08306b",7)))
plot(x, maxpixels = ncell(x), col = col, colNA = "#818181", main = "Bathymetry", axes = FALSE, ylim=extent(x)[3:4])
dev.off()


# Version 2; writing to png

if(!requireNamespace("leaflet")) {
  install.packages("leaflet")
}
if(!requireNamespace("sdmpredictors")) {
  install.packages("sdmpredictors")
}
library(sdmpredictors)
library(raster)

# create colors
colors <- leaflet::colorNumeric(rev(c("#f7fbff", "#deebf7", "#c6dbef", "#9ecae1", "#6baed6", "#4292c6","#2171b5","#08519c", "#08306b")),
                                -1:1001, na.color =  "#818181")
cols <- c(colors(-1:1001), colors(NA))
x <- sdmpredictors::load_layers("BO_bathymean", equalarea = TRUE)
# scale values and remove extreme values from the color range 
vals <- values(x)
vals <- scale(vals)
minmax <- quantile(vals, probs=c(0.01, 0.99), na.rm = TRUE)
vals <- round((((vals - minmax[1]) / (minmax[2] - minmax[1])) * 1000))
vals[vals < 0] <- 0
vals[vals > 1000] <- 1000
vals[is.na(vals)] <- 1002

# lookup colors for scaled values, convert to raw and write to file
valcolors <- cols[vals+2] # +2 because -1 and 0 are in cols (value 0 is at index 2 in cols)

rgb_data <- col2rgb(valcolors, alpha = TRUE)
raw_data <- as.raw(rgb_data)
dim(raw_data) <- c(4, ncol(x), nrow(x))

png::writePNG(raw_data, "bathymetry_plot2.png")