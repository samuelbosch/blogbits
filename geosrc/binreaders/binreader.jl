function readvalue(stream, position)
    seek(stream, position)
    return read(stream, Int32)
end

function readvalues(filename::String, indices)
    stream = open(filename, "r")
    try
        return Int32[readvalue(stream, index*4) for index in indices]
    finally
        close(stream)
    end
end

function getindices(n)
    [10000+((i-1)*3) for i=1:n]
end

function smallmarspec(outer, inner)
  for i=1:outer
    r = readvalues("D:\\temp\\bathy_10m.sbg", getindices(inner))
  end
end

function allmarspec(outer, inner)
    paths = ["bathy_10m.sbg", "biogeo01_aspect_EW_10m.sbg", "biogeo02_aspect_NS_10m.sbg", "biogeo03_plan_curvature_10m.sbg", "biogeo04_profile_curvature_10m.sbg", "biogeo05_dist_shore_10m.sbg", "biogeo06_bathy_slope_10m.sbg", "biogeo07_concavity_10m.sbg", "biogeo08_sss_mean_10m.sbg", "biogeo09_sss_min_10m.sbg", 
            "biogeo10_sss_max_10m.sbg", "biogeo11_sss_range_10m.sbg", "biogeo12_sss_variance_10m.sbg", "biogeo13_sst_mean_10m.sbg", "biogeo14_sst_min_10m.sbg", "biogeo15_sst_max_10m.sbg", "biogeo16_sst_range_10m.sbg", "biogeo17_sst_variance_10m.sbg", "sss01_10m.sbg", "sss02_10m.sbg", "sss03_10m.sbg", "sss04_10m.sbg", "sss05_10m.sbg", 
            "sss06_10m.sbg", "sss07_10m.sbg", "sss08_10m.sbg", "sss09_10m.sbg", "sss10_10m.sbg", "sss11_10m.sbg", "sss12_10m.sbg", "sst01_10m.sbg", "sst02_10m.sbg", "sst03_10m.sbg", "sst04_10m.sbg", "sst05_10m.sbg", "sst06_10m.sbg", "sst07_10m.sbg", "sst08_10m.sbg", "sst09_10m.sbg"]
    indices = getindices(inner)
    r = []
    for i=1:outer
        r = [readvalues(string("D:\\temp\\",path), indices) for path in paths]
    end
    r
end

@elapsed allmarspec(10,10) # <0.09s
@elapsed allmarspec(100,100) # <5s
@elapsed allmarspec(1000,100) # 45s
@elapsed allmarspec(10,10000) # 41s
@elapsed allmarspec(1,100000) # 46s