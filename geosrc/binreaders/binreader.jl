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
    paths = ["D:\\temp\\bathy_10m.sbg", "D:\\temp\\bathy_10m_plus_300.sbg", "D:\\temp\\biogeo01_aspect_EW_10m.sbg", "D:\\temp\\biogeo02_aspect_NS_10m.sbg", "D:\\temp\\biogeo03_plan_curvature_10m.sbg", "D:\\temp\\biogeo04_profile_curvature_10m.sbg", "D:\\temp\\biogeo05_dist_shore_10m.sbg", "D:\\temp\\biogeo06_bathy_slope_10m.sbg", "D:\\temp\\biogeo07_concavity_10m.sbg", "D:\\temp\\biogeo08_sss_mean_10m.sbg", "D:\\temp\\biogeo09_sss_min_10m.sbg", 
            "D:\\temp\\biogeo10_sss_max_10m.sbg", "D:\\temp\\biogeo11_sss_range_10m.sbg", "D:\\temp\\biogeo12_sss_variance_10m.sbg", "D:\\temp\\biogeo13_sst_mean_10m.sbg", "D:\\temp\\biogeo14_sst_min_10m.sbg", "D:\\temp\\biogeo15_sst_max_10m.sbg", "D:\\temp\\biogeo16_sst_range_10m.sbg", "D:\\temp\\biogeo17_sst_variance_10m.sbg", "D:\\temp\\sss01_10m.sbg", "D:\\temp\\sss02_10m.sbg", "D:\\temp\\sss03_10m.sbg", "D:\\temp\\sss04_10m.sbg", "D:\\temp\\sss05_10m.sbg", 
            "D:\\temp\\sss06_10m.sbg", "D:\\temp\\sss07_10m.sbg", "D:\\temp\\sss08_10m.sbg", "D:\\temp\\sss09_10m.sbg", "D:\\temp\\sss10_10m.sbg", "D:\\temp\\sss11_10m.sbg", "D:\\temp\\sss12_10m.sbg", "D:\\temp\\sst01_10m.sbg", "D:\\temp\\sst02_10m.sbg", "D:\\temp\\sst03_10m.sbg", "D:\\temp\\sst04_10m.sbg", "D:\\temp\\sst05_10m.sbg", "D:\\temp\\sst06_10m.sbg", "D:\\temp\\sst07_10m.sbg", "D:\\temp\\sst08_10m.sbg", "D:\\temp\\sst09_10m.sbg"]
    indices = getindices(inner)
    for i=1:outer
        for path in paths
            r = readvalues(path, indices)
        end
    end
end

@elapsed allmarspec(10,10) # <0.09s
@elapsed allmarspec(100,100) # <5s
@elapsed allmarspec(1000,100) # 45s
@elapsed allmarspec(10,10000) # 41s
@elapsed allmarspec(1,100000) # 46s