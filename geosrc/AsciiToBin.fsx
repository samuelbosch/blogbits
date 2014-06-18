
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

    type bitmap = System.Collections.BitArray
    let init (bools:bool []) = new System.Collections.BitArray(bools)

    let isSet index (b:bitmap) =
        b.Get(int index)

    let countUpto (uptoIndex:int64) (b:bitmap) = 
        let mutable count = 0L
        for i=0 to (int uptoIndex)-1 do
            if b.Get(i) then
                count <- count+1L
        count

    let countSkippedUpto (uptoIndex:int64) (b:bitmap) =
        let count = countUpto uptoIndex b
        uptoIndex - count

    let sparseIndex (map:bitmap[]) (cellIndex:int64) =
        let ncols = int64 map.[0].Length
        let rowIndex = cellIndex / ncols
        let colIndex = (cellIndex - (rowIndex*ncols))
        if isSet colIndex map.[int rowIndex] then
            let skippedCount = map |> Seq.take (int rowIndex) |> Seq.map ((countSkippedUpto ncols)>>int64) |> Seq.sum
            let skippedCount = skippedCount + int64 (countSkippedUpto colIndex map.[int rowIndex])
            Some(cellIndex - skippedCount)
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
    let writeBitMap fileName (map:bitmap []) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        let nrows = map.Length
        let ncols = map.[0].Length
        writer.Write(nrows)
        writer.Write(ncols)

        let boolArray = Array.init ncols (fun i -> true)
        for bitarray in map do
            bitarray.CopyTo(boolArray, 0)
            boolArray |> Array.iter (fun b -> (writer.Write(b))) 

    let writeValues fileName (values:seq<int option>) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        values |> Seq.iter (fun v -> if v.IsSome then writer.Write(v.Value))

//    let readValue (reader:BinaryReader)  cellIndex = 
//        // set stream to correct location
//        reader.BaseStream.Position <- cellIndex*4L
//        match reader.ReadInt32() with
//        | Int32.MinValue -> None
//        | v -> Some(v)
        
    let readValue (map:bitmap []) (reader:BinaryReader)  cellIndex =
        match BitMap.sparseIndex map cellIndex with
        | Some(index) -> SimpleReadWrite.readValue reader index
        | None -> None
        
    let readValues (map:bitmap []) valuesFileName indices = 
        use reader = new BinaryReader(File.Open(valuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
        let values = 
            indices
            |> List.ofSeq
            
//        let values = List.map (readValue reader) (List.ofSeq indices)
        values

module AsciiProvider = 
    let parseValue nodata (v:string) =
        if v <> nodata then
            Some(int v)
        else
            None

    let parseRow nodata (values:string []) =
        let parsed = values |> Array.map (parseValue nodata)
        let bitmap = parsed |> Array.map Option.isSome |> BitMap.init
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
        let (bitmap, values) = read inFileName
        converter (outFileName+extension) bitmap values // (Seq.concat values) bitmap

    let convertToSbg = 
        convertTo ".sbg" (fun outfile _ values -> SimpleReadWrite.writeValues outfile (Seq.concat values))
    
//    let convertToSbg2 =
//        convertTo ".sbg2" SimpleReadWrite.writeValues

module SbgReader =
    let query indices fileName =
        //SimpleReadWrite.readValues fileName indices
        MemoryMappedSimpleRead.readValues fileName indices
     
    let testPrepareLoad fileName = 
        let sbg = Path.Combine(@"D:\temp\", Path.GetFileNameWithoutExtension(fileName))
        if (not (File.Exists(sbg))) then
            AsciiProvider.convertToSbg fileName sbg
        sbg

    let testRandomQuery name paths outerlen innerlen =
        printfn "START %s" name
        let sbgPaths = paths |> Array.map testPrepareLoad
        
        let r = new System.Random(1)
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

    let testSmallMarspec() =
        let result = testRandomQuery "testSmallMarspec" [|@"D:\a\data\marspec\MARSPEC_10m\ascii\bathy_10m.asc"|] 10 10000 
        printfn "result: %A" result
    let testAllBioOracle() =
        ignore // TODO

    let testAllMarspec10m() =
        // fetch 1000 times, 100 random values from 40 marspec layers

        let root = @"D:\a\data\marspec\MARSPEC_10m\ascii\"
        let paths = Directory.GetFiles(root, "*.asc")
        testRandomQuery "testAllMarspec10m" paths 100 1000 |> ignore
        // 39s 10000 * 10
        // 22s 1000 * 100
        // 17s 100 * 1000
        // 16s 100 * 1000 with BinaryReader caching
        // MemoryMappedRead
        // 86s 10000 * 10
        // 28s 1000 * 100
        // 10s 100*1000
        // 9s 100*1000 with MemoryMappedFile caching
        

    let testGebco() =
        ignore // TODO

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

    let runall() =
        simpleReadWriteTest()
        memoryMappedReadTest()
        SbgReader.testSmallMarspec()
        SbgReader.testAllMarspec10m()
Test.runall()
        

        