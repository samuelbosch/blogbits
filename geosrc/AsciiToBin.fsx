open System
open System.IO
open System.Collections.Generic
open System.Collections
open System.Linq

module BitConverter = 
    let pow2 y = 1 <<< y
    // convert booleans to bytes in a space efficient way
    let FromBooleans (bools:bool []) =
        seq {
            let b = ref 0uy
            for i=0 to bools.Length-1 do
                let rem = (i  % 8)
                if rem = 0 && i<> 0 then 
                    yield !b
                    b := 0uy
                if bools.[i] then
                    b := !b + (byte (pow2 rem))
            yield !b
        } |> Array.ofSeq
    // to booleans only works for bytes created with FromBooleans
    let ToBooleans (bytes:byte []) = 
        bytes
        |> Array.map (fun b -> Array.init 8 (fun i -> ((pow2 i) &&& int b) > 0))
        |> Array.concat

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
    open BitMap
    let bitmapExtension = ".sbm" // sparse binary map
    let valuesExtension = ".sbv" // sparse binary values

    let pow2 y = 1 <<< y

    let writeBooleans (bools:bool []) (writer:BinaryWriter) =
        BitConverter.FromBooleans bools
        |> (fun bytes -> writer.Write(bytes))

    let writeBitMap fileName (map:bitmap []) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        let nrows = map.Length
        let ncols = (fst map.[0]).Length
        writer.Write(nrows)
        writer.Write(ncols)
        for (boolArray,_) in map do
            writeBooleans boolArray writer

    let writeValues fileName (values:seq<int option>) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        values |> Seq.iter (fun v -> if v.IsSome then writer.Write(v.Value))

    let write filenameWithoutExtension bitmap values =
        writeBitMap (Path.ChangeExtension(filenameWithoutExtension, bitmapExtension)) bitmap
        let valuesPath = (Path.ChangeExtension(filenameWithoutExtension, valuesExtension))
        writeValues valuesPath values
    
    let cache = new Dictionary<string, bitmap []>()

    let readBooleansRow ncols (reader:BinaryReader) =
        let nbytes = int (ceil ((float ncols) / 8.0)) // n° of bytes in a row
        Array.init nbytes (fun i -> reader.ReadByte())
        |> BitConverter.ToBooleans

    let readBitMap bitMapFileName = 
        if cache.ContainsKey(bitMapFileName) then 
            cache.[bitMapFileName] 
        else
            printfn "readBitMap %s" bitMapFileName
            use reader = new BinaryReader(File.Open(bitMapFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            let nrows = reader.ReadInt32()
            let ncols = reader.ReadInt32()
            let createRowBitMap i = BitMap.ofArray (readBooleansRow ncols reader)
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
        let map = readBitMap (Path.ChangeExtension(valuesFileName, bitmapExtension))
        use reader = new BinaryReader(File.Open(valuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
        let values = 
            indices
            |> List.ofSeq
            |> List.map (readValue map reader)
        values

module BatchOrderedRead = // asummes sorted and distinct input

    let readValuesBatch (reader:BinaryReader) (indices:int64 []) = 
        // set stream to correct location
        let first,last = indices.[0], (indices.[indices.Length-1])
        reader.BaseStream.Position <- first*4L
        let bytes = reader.ReadBytes(int (((last + 1L) - first) * 4L))
        let readValue index =
            match (BitConverter.ToInt32(bytes, int (index-first))) with 
            | Int32.MinValue -> None 
            | v -> Some(v)

        Array.map readValue indices
        
    let readValues fileName indices = 
        use reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
        let batchSize = 400L
        let folder (first,lists) i = 
            if ((i - first) < batchSize) && ((i - first) >= 0L) then 
                match lists with
                | x::xs -> (first, (i::x)::xs)
                | [] -> (i, [[i]])
            else
                (i, ([i]::lists))
            
        let grouped = indices |> Seq.fold folder (-1L,[]) |> snd |> List.map (List.rev>>Array.ofList) |> List.rev
        grouped |> List.map (readValuesBatch reader)

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
        SimpleReadWrite.readValues fileName indices
        //MemoryMappedSimpleRead.readValues fileName indices

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
        printfn "START %s %d %d" name outerlen innerlen
        let sbgPaths = paths |> Array.map (testPrepareLoad converter)
        
        let r = new Random(1)
        let sw = new System.Diagnostics.Stopwatch()
        let len = outerlen
        let arr : int64 [] = Array.zeroCreate len
        let mutable result = null
        for i=0 to len-1 do
            sw.Restart()
            let indices = [1..innerlen] |> List.map (fun i -> (int64 (r.Next(0, 2160*1080)) ))
            //sorted and distinct input is assumed by some readers
            let indices = List.sort indices |> Seq.distinct |> List.ofSeq
            use t = File.CreateText(@"D:\test_param.txt")
            result <- Array.map (query indices) sbgPaths
            sw.Stop()
            arr.[i] <- sw.ElapsedMilliseconds

        printfn "avg %f ms" (Array.averageBy float arr)
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

    let testSimpleRandom = testRandomQuery AsciiProvider.convertToSimple (fun indices fileName -> SimpleReadWrite.readValues fileName indices)
    let testMemoryRandom = testRandomQuery AsciiProvider.convertToSimple (fun indices fileName -> MemoryMappedSimpleRead.readValues fileName indices)
    let testSparseRandom = testRandomQuery AsciiProvider.convertToSparse Reader.sparseQuery 
    let testBatchRandom = testRandomQuery AsciiProvider.convertToSimple (fun indices fileName -> BatchOrderedRead.readValues fileName indices)

    let testSmallMarspec() =
        let outerlen = 10
        let innerlen = 10000
        let result3 = testBatchRandom "test batch SmallMarspec" [|@"D:\a\data\marspec\MARSPEC_10m\ascii\bathy_10m.asc"|] outerlen innerlen
        let result1 = testSimpleRandom "test simple SmallMarspec" [|@"D:\a\data\marspec\MARSPEC_10m\ascii\bathy_10m.asc"|] outerlen innerlen
        let result2 = testSparseRandom "test sparse SmallMarspec" [|@"D:\a\data\marspec\MARSPEC_10m\ascii\bathy_10m.asc"|] outerlen innerlen
                
//        printfn "simple result: %A" result1
//        printfn "sparse result: %A" result2
//        printfn "batch result: %A" result2
        ()

    let testAllBioOracle() =
        ignore // TODO
    let writeParams innerlen (outerlen:int) = 
        let r = new Random(1)

        use t = File.AppendText(@"D:\test_param.txt")
        t.WriteLine(outerlen)

        for i=1 to outerlen do
            
            let indices = [1..innerlen] |> List.map (fun i -> (int64 (r.Next(0, 2160*1080)) ))
            //sorted and distinct input is assumed by some readers
            let indices = List.sort indices |> Seq.distinct |> List.ofSeq
            t.WriteLine(indices.Length)
            t.WriteLine(String.Join(";", (List.map string indices)))
            
    let testAllMarspec10m() =
        // fetch 1000 times, 100 random values from 40 marspec layers

        let root = @"D:\a\data\marspec\MARSPEC_10m\ascii\"
        let paths = Directory.GetFiles(root, "*.asc")
        let prm = [(10000,10);(1000,100);(100,1000)]
        for (outer, inner) in prm do
            testSimpleRandom "Simple AllMarspec10m" paths outer inner |> ignore
            testMemoryRandom "Memory mapped AllMarspec10m" paths outer inner |> ignore
            testSparseRandom "Sparse AllMarspec10m" paths outer inner |> ignore
            testBatchRandom "Batch AllMarspec10m" paths outer inner |> ignore
        // Simple
        // 32s 10000 * 10
        // 20s 1000 * 100
        // 14s 100 * 1000
        // 13s 100 * 1000 with BinaryReader caching
        // MemoryMappedRead
        // 109s 10000 * 10
        // 23s 1000 * 100
        // 6s 100*1000
        // 9s 100*1000 with MemoryMappedFile caching
        // Sparse (all with bitmap caching up front)
        // 32s 10000 * 10
        // 17s 1000 * 100
        // 14s 100 * 1000
        // Batch
        // 34s 10000 * 10
        // 20s 1000 * 100
        // 15s 100 * 1000

    let testGebco() =
        ignore // TODO

    let runall() =
        simpleReadWriteTest()
        memoryMappedReadTest()
        testSmallMarspec()
        testAllMarspec10m()

//Test.runall()


module Seq =
    let all predicate source =
        not (Seq.exists (predicate>>not) source)
    let iterseq f (sources: seq<seq<_>>) =
        let enumerators = sources |> Array.ofSeq |> Array.map (fun s -> (s.GetEnumerator()))
        while (enumerators |> all (fun e -> (e.MoveNext()))) do
            enumerators |> Array.iter (fun enumerator -> f (enumerator.Current))


module MergedReadWrite = 
    let merge dir =
        let files = System.IO.Directory.EnumerateFiles(dir, "*.sbg") |> List.ofSeq // force creation
        let outfile = System.IO.Path.GetDirectoryName(Seq.head files) + @"\merged.mbg"
        use writer = new BinaryWriter(File.Open(outfile, FileMode.OpenOrCreate)) 
        let readers = files |> Seq.map (fun f -> (new BinaryReader(File.Open(f,FileMode.Open, FileAccess.Read, FileShare.Read))))
        
        let read (reader:BinaryReader) =
            seq {
                let b = ref (reader.ReadBytes(4))
                while (!b).Length > 0 do
                    yield !b
                    b := reader.ReadBytes(4)
            }
        
        let values = readers |> Seq.map read
        values |> Seq.iterseq (fun b -> (writer.Write(b))) 
        //(read (Seq.head readers)) |> Seq.iter (fun b -> (writer.Write(b)))
            
    let readColumns (reader:BinaryReader) columnCount cellIndex = 
        // set stream to correct location
        reader.BaseStream.Position <- cellIndex*4L*(int64 columnCount)
        let read i = match reader.ReadInt32() with
                     | Int32.MinValue -> None
                     | v -> Some(v)
        let values:int option[] = Array.init columnCount read
        values
//        let bytes = reader.ReadBytes(4*columnCount)
//        let values = Array.init columnCount (fun i -> (int bytes.[0] ||| (int bytes.[1] <<< 8) ||| (int bytes.[2] <<< 16) ||| (int bytes.[3] <<< 24)))
//        values |> Array.map (fun i -> if (i = Int32.MinValue) then None else Some(i))


    let readValues fileName ncol indices = 
        use reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
        let values = indices |> Array.map (readColumns reader ncol)
        values

    let testRandomMergeQuery path ncol outerlen innerlen =
        printfn "START random merge query %d %d" outerlen innerlen
        
        let r = new Random(1)
        let sw = new System.Diagnostics.Stopwatch()
        let len = outerlen
        let arr : int64 [] = Array.zeroCreate len
        let mutable result = null
        for i=0 to len-1 do
            sw.Restart()
            let indices = [1..innerlen] |> List.map (fun i -> (int64 (r.Next(0, 2160*1080)) ))
            //sorted and distinct input is assumed by some readers
            let indices = List.sort indices |> Seq.distinct |> Array.ofSeq
            result <- readValues path ncol indices
            sw.Stop()
            arr.[i] <- sw.ElapsedMilliseconds

        printfn "avg %f ms" (Array.averageBy float arr)
        printfn "max %d ms" (Array.max arr)
        printfn "sum %d ms" (Array.sum arr)
        result

    let testMultiParams() =
        let prm = [(10000,10);(1000,100);(100,1000);(10,100000);(1,1000000)]
        for (outer, inner) in prm do
            printfn "length: %d" (testRandomMergeQuery @"D:\temp\sbg_10m\merged.mbg" 39 outer inner |> Array.length)
