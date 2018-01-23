library(sdmpredictors)
library(raster)

coast_mask <- function(layer) {
  edges <- raster::boundaries(raster(layer, layer=1), type="inner")
  values <- getValues(edges)
  is.na(values) | values == 0
}

l <- load_layers("BO_sstmean", equalarea = FALSE)

mask <- coast_mask(l)
l[mask] <- NA

plot(l, col=rev(heat.colors(255)))
