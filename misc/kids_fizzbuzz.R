generate_sums <- function(maxresult=10) {
  sums <- c()
  for (i in 0:(maxresult-1)) {
    for (j in 0:(maxresult-i)) {
      sums <- c(sums, paste0(i, ' + ', j, ' = ?'))
    }
  }
  sample(sums[!duplicated(sums)])
}
generate_subtractions <- function(maxresult=10) {
  substractions <- c()
  for (i in 0:maxresult) {
    for (j in i:maxresult) {
      substractions <- c(substractions, paste0(j, ' - ', i, ' = ?'))
    }
  }
  sample(substractions[!duplicated(substractions)])
}
