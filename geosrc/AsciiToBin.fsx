open System
open System.IO
open System.Collections.Generic
open System.Collections
open System.Linq

module Dict = 

    let tryGetDefault (d:IDictionary<'k,'v>) (key:'k) (defaultValue:'v) =
        if d.ContainsKey(key) then
            d.[key]
        else
            defaultValue

module BitMap =
    type bitmap = bool [] * int64 // map * skipped count
    
    let ofArray (bools:bool []) = 
        let skippedCount = bools |> Seq.filter (not) |> Seq.length
        (bools, (int64 skippedCount)):bitmap
    
    let transform (b:bitmap []) = 
        let mutable total = 0L
        for i=0 to b.Length-1 do
            let (m,s):bitmap = b.[i] 
            total <- total+s
            b.[i] <- (m,total)

    let init n (f:int->bool) = 
        let bools = Array.init n f
        ofArray bools

    let sparseIndex sparseIndexConsumer (map:bitmap[]) (cellIndex:int64) =
        let ncols = int64 (fst map.[0]).Length
        let rowIndex = int (cellIndex / ncols)
        let colIndex = int (cellIndex - ((int64 rowIndex)*ncols))
        if (fst map.[rowIndex]).[colIndex] then
            let mutable skippedCount = 0L
            if rowIndex > 0 then
                skippedCount <- (snd map.[rowIndex-1])
            let m = fst  map.[rowIndex]
            for i=0 to colIndex-1 do
                if not (m.[i]) then
                    skippedCount <- skippedCount+1L

            Some(sparseIndexConsumer (cellIndex - skippedCount))
        else
            None

module PSeqOrdered =

    let map (mapping:'a->'b) (source:seq<'a>) =
        source.AsParallel().AsOrdered().Select(mapping)

module SimpleReadWrite = 

    let writeValue (writer:BinaryWriter) (value:int option) =
        match value with
        | Some(v) -> writer.Write(v)
        | None -> writer.Write(Int32.MinValue)

    let writeValues fileName (values:seq<int option>) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        values
        |> Seq.iter (writeValue writer)
            
    let readValue (reader:BinaryReader) cellIndex = 
        // set stream to correct location
        reader.BaseStream.Position <- cellIndex*4L
        match reader.ReadInt32() with
        | Int32.MinValue -> None
        | v -> Some(v)
        
    let readValues fileName indices = 
        use reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
        let values = List.map (readValue reader) (List.ofSeq indices)
        values

module MemoryMappedSimpleRead =

    open System.IO.MemoryMappedFiles

    let readValue (reader:MemoryMappedViewAccessor) offset cellIndex =
        let position = (cellIndex*4L) - offset
        match reader.ReadInt32(position) with
        | Int32.MinValue -> None
        | v -> Some(v)
        
    let readValues fileName indices =
        use mmf = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open)
        let offset = (Seq.min indices ) * 4L
        let last = (Seq.max indices) * 4L
        let length = 4L+last-offset
        use reader = mmf.CreateViewAccessor(offset, length, MemoryMappedFileAccess.Read)
        let values = (List.ofSeq indices) |> List.map (readValue reader offset)
        values

module SparseReadWrite = 
    let time n f =
        let sw = Diagnostics.Stopwatch.StartNew()
        let x = f()
        printfn "%s took %d ms" n (sw.ElapsedMilliseconds)
        x
    open BitMap
    let bitmapExtension = ".sbm" // sparse binary map
    let valuesExtension = ".sbv" // sparse binary values
    let writeBitMap fileName (map:bitmap []) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        let nrows = map.Length
        let ncols = (fst map.[0]).Length
        writer.Write(nrows)
        writer.Write(ncols)

        let boolArray = Array.init ncols (fun i -> true)
        for (bitarray,_) in map do
            bitarray.CopyTo(boolArray, 0)
            boolArray |> Array.iter (fun b -> (writer.Write(b))) 

    let writeValues fileName (values:seq<int option>) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        values |> Seq.iter (fun v -> if v.IsSome then writer.Write(v.Value))

    let write filenameWithoutExtension bitmap values =
        writeBitMap (Path.ChangeExtension(filenameWithoutExtension, bitmapExtension)) bitmap
        let valuesPath = (Path.ChangeExtension(filenameWithoutExtension, valuesExtension))
        writeValues valuesPath values
    
    let cache = new Dictionary<string, bitmap []>()

    let readBitMap bitMapFileName = 
        if cache.ContainsKey(bitMapFileName) then 
            cache.[bitMapFileName] 
        else
            printfn "readBitMap %s" bitMapFileName
            use reader = new BinaryReader(File.Open(bitMapFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            let nrows = reader.ReadInt32()
            let ncols = reader.ReadInt32()
            let createRowBitMap i = BitMap.init ncols (fun i -> reader.ReadBoolean())
            let map = Array.init nrows createRowBitMap
            BitMap.transform map
            cache.Add(bitMapFileName, map)
            map

    let readValue (map:bitmap []) (reader:BinaryReader) cellIndex =
        let read sparseIndex = 
            reader.BaseStream.Position <- sparseIndex*4L
            reader.ReadInt32()
        BitMap.sparseIndex read map cellIndex

    let readValues valuesFileName indices = 
        let map = time "readBitMap" (fun () -> readBitMap (Path.ChangeExtension(valuesFileName, bitmapExtension)))
        use reader = new BinaryReader(File.Open(valuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
        let values = 
            indices
            |> List.ofSeq
            |> List.map (readValue map reader)
        values

module AsciiProvider = 
    let parseValue nodata (v:string) =
        if v <> nodata then
            Some(int v)
        else
            None

    let parseRow nodata (values:string []) =
        let parsed = values |> Array.map (parseValue nodata)
        let bitmap = parsed |> Array.map Option.isSome |> BitMap.ofArray
        (bitmap, parsed)

    let read fileName =
        let lines = File.ReadLines(fileName)
        let isHeader (l:string) = (l.Length < 1000)
        let header = lines.TakeWhile(isHeader).ToDictionary((fun (l:string) -> l.Split([|' '|], StringSplitOptions.RemoveEmptyEntries).[0]), (fun (l:string) -> l.TrimEnd([|'\n'|]).Split([|' '|]).Last()))
        let nodata = Dict.tryGetDefault header "NODATA_value" "-99999"

        let inline splitLine (x:string) = x.Trim([|' '; '\n'|]).Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        
        let bitmap, values = 
            lines.SkipWhile(isHeader)
            |> PSeqOrdered.map (splitLine >> (parseRow nodata)) // PSeq changes order of the sequence
            |> Array.ofSeq
            |> Array.unzip
        bitmap, values

    let convertTo extension converter inFileName outFileName =
        let outFileName = Path.ChangeExtension(outFileName, extension)
        if (not (File.Exists(outFileName))) then
            let (bitmap, values) = read inFileName
            converter (outFileName) bitmap (Seq.concat values) // (Seq.concat values) bitmap
        outFileName

    let convertToSimple = 
        convertTo ".sbg" (fun outfile _ values -> SimpleReadWrite.writeValues outfile values)
    
    let convertToSparse = 
        convertTo SparseReadWrite.valuesExtension SparseReadWrite.write
    
module Reader =
    let simpleQuery indices fileName =
        //SimpleReadWrite.readValues fileName indices
        MemoryMappedSimpleRead.readValues fileName indices

    let sparseQuery indices fileName =
        SparseReadWrite.readValues fileName indices

module Test =
    let simpleReadWriteTest() = 
        let fileName = @"D:\temp\testSimpleReadWrite.sbg" // *.sbg simple binary grid
        let initial = [None;Some(Int32.MinValue+1);Some(Int32.MaxValue);None;Some(0);Some(1);Some(-1);None;Some(2);Some(213);None]

        SimpleReadWrite.writeValues fileName initial
        let expected = Seq.concat ([[initial.[4];initial.[3];initial.[4];initial.[4];initial.[2]];initial])
        let actual = SimpleReadWrite.readValues fileName (Seq.concat [[4L;3L;4L;4L;2L];[0L..(initial.LongCount()-1L)]])
        let result = Seq.zip actual expected |> Seq.forall (fun (a, b) -> a = b)
        printfn "Simple read write test returned %b" result

    let memoryMappedReadTest() =
        let fileName = @"D:\temp\testSimpleReadWrite.sbg" // *.sbg simple binary grid
        let initial = [None;Some(Int32.MinValue+1);Some(Int32.MaxValue);None;Some(0);Some(1);Some(-1);None;Some(2);Some(213);None]
        let expected = Seq.concat ([[initial.[4];initial.[3];initial.[4];initial.[4];initial.[2]];initial])
        let actual = MemoryMappedSimpleRead.readValues fileName (Seq.concat [[4L;3L;4L;4L;2L];[0L..(initial.LongCount()-1L)]])
        let result = Seq.zip actual expected |> Seq.forall (fun (a, b) -> a = b)
        printfn "Memory mapped read test returned %b" result

    let sparseReadWriteTest() =
        let p = @"D:\temp\bathy_10m.asc"
        let (bitmap, values) = AsciiProvider.read p
        let p = AsciiProvider.convertToSparse p (Path.ChangeExtension(p, ""))
        let results = SparseReadWrite.readValues p [1L;1654L;649L;963L]
        ignore

    let testPrepareLoad converter fileName = 
        let sbg = Path.Combine(@"D:\temp\", Path.GetFileNameWithoutExtension(fileName))
        converter fileName sbg

    let testRandomQuery converter query name paths outerlen innerlen =
        printfn "START %s" name
        let sbgPaths = paths |> Array.map (testPrepareLoad converter)
        
        let r = new Random(1)
        let sw = new System.Diagnostics.Stopwatch()
        let len = outerlen
        let arr : int64 [] = Array.zeroCreate len
        let mutable result = null
        for i=0 to len-1 do
            sw.Restart()
            let indices = [1..innerlen] |> List.map (fun i -> (int64 (r.Next(0, 2160*1080)) ))
            let indices = indices.OrderBy(fun x -> x)
            result <- Array.map (query indices) sbgPaths
            sw.Stop()
            arr.[i] <- sw.ElapsedMilliseconds

        printfn "avg %f ms" (Array.averageBy float arr)
        printfn "min %d ms" (Array.min arr)
        printfn "max %d ms" (Array.max arr)
        printfn "sum %d ms" (Array.sum arr)
        result

    let testSparseIndex() = 
        let r = new Random(1)
        
        let innerlen = 10000
        let indices = [1..innerlen] |> List.map (fun i -> (int64 (r.Next(0, 2160*1080)) ))
        let map = SparseReadWrite.readBitMap @"D:\temp\bathy_10m.sbm"
        let sw = Diagnostics.Stopwatch.StartNew()
        let result = indices |> Seq.map (BitMap.sparseIndex id map) |> Array.ofSeq
        sw.Stop()
        printfn "test sparse index %d ms" sw.ElapsedMilliseconds
        result

    let testSimpleRandom = testRandomQuery AsciiProvider.convertToSimple Reader.simpleQuery
    let testSparseRandom = testRandomQuery AsciiProvider.convertToSparse Reader.sparseQuery 
    let testSmallMarspec() =
        let outerlen = 10
        let innerlen = 10000
        let result1 = testSimpleRandom "test simple SmallMarspec" [|@"D:\a\data\marspec\MARSPEC_10m\ascii\bathy_10m.asc"|] outerlen innerlen
        let result2 = testSparseRandom "test sparse SmallMarspec" [|@"D:\a\data\marspec\MARSPEC_10m\ascii\bathy_10m.asc"|] outerlen innerlen
        printfn "simple result: %A" result1
        printfn "sparse result: %A" result2

    let testAllBioOracle() =
        ignore // TODO

    let testAllMarspec10m() =
        // fetch 1000 times, 100 random values from 40 marspec layers

        let root = @"D:\a\data\marspec\MARSPEC_10m\ascii\"
        let paths = Directory.GetFiles(root, "*.asc")
        testSimpleRandom "test simple AllMarspec10m" paths 100 1000 |> ignore
        //testSparseRandom "test sparse AllMarspec10m" paths 10000 10 |> ignore
        
        // Simple
        // 39s 10000 * 10
        // 22s 1000 * 100
        // 17s 100 * 1000
        // 16s 100 * 1000 with BinaryReader caching
        // MemoryMappedRead
        // 86s 10000 * 10
        // 28s 1000 * 100
        // 10s 100*1000
        // 9s 100*1000 with MemoryMappedFile caching
        // Sparse
        
    let testGebco() =
        ignore // TODO

    let runall() =
        simpleReadWriteTest()
        memoryMappedReadTest()
        testSmallMarspec()
        testAllMarspec10m()

//Test.runall()