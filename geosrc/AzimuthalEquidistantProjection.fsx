open System
module AzimuthalEquidistantProjection =

    let inline degToRad d = 0.0174532925199433 * d; // (1.0/180.0 * Math.PI) * d

    let project centerlon centerlat lon lat =
        // http://mathworld.wolfram.com/AzimuthalEquidistantProjection.html
        // http://www.radicalcartography.net/?projectionref
        let t:float = degToRad lat
        let l:float = degToRad lon
        let t1 = degToRad centerlat // latitude center of projection
        let l0 = degToRad centerlon // longitude center of projection
        let c = Math.Acos ((sin t1) * (sin t) + (cos t1) * (cos t) * (cos (l-l0)))
        let k = c / (sin c)
        let x = k * (cos t) * (sin (l-l0))
        let y = k * (cos t1) * (sin t) - (sin t1) * (cos t) * (cos (l-l0))
        (x, y)

    let project_optimized l0 t1 cost1 lon lat =
        // http://mathworld.wolfram.com/AzimuthalEquidistantProjection.html
        // http://www.radicalcartography.net/?projectionref
        let t:float = degToRad lat
        let l:float = degToRad lon
        
        let costcosll0 = (cos t) * (cos (l-l0))
        let sint = sin t
        let sint1 = sin t1
        let c = Math.Acos ((sint1) * (sint) + (cost1) * costcosll0)
        let k = c / (sin c)
        let x = k * (cos t) * (sin (l-l0))
        let y = k * (cost1) * (sint) - (sint1) * costcosll0
        (x, y)

    let buildProjection centerLon centerLat = 
        let t1 = degToRad centerLat
        let l0 = degToRad centerLon
        let cost1 = cos t1
        project_optimized l0 t1 cost1