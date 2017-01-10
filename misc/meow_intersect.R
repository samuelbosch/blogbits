library(sp)
library(rgdal)

get_ecoregions <- function(data, xy_cols, meow_path) {
  df <- data
  row.names(df) <- 1:nrow(df)
  coordinates(data) <- xy_cols
  data@proj4string <-  CRS("+proj=longlat +datum=WGS84 +ellps=WGS84 +towgs84=0,0,0")
  layer <- tools::file_path_sans_ext(basename(meow_path))
  meow <- readOGR(dsn = meow_path, layer = layer,verbose = FALSE, stringsAsFactors = FALSE)
  meow@proj4string <- data@proj4string # set the same, because they are
  intersect_meow_df <- gIntersection(meow, data, byid = TRUE)
  id_pairs <- strsplit(row.names(intersect_meow_df), " ")
  id_pairs_mat <- as.matrix(do.call(rbind, id_pairs))
  ecoids <- id_pairs_mat[,1]
  names(ecoids) <- id_pairs_mat[,2] ## store rowname from df.sp in names(ecoids)
  meow_row_names <- ecoids[row.names(data)]
  meow_row_names[is.na(meow_row_names)] <- "invalid row name"
  cbind(df, meow@data[meow_row_names,])
}

# df <- data.frame(species=rep("a", 4), longitude=c(4.087523,3.66044,2.926591, 0), latitude=c(51.671932, 51.55974, 51.234695,0))
# m <- get_ecoregions(df, c("longitude", "latitude"), "D:/a/data/ecoregions/meow_ecos.shp")

