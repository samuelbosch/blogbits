open System
open System.IO
open System.Collections.Generic
open System.Collections
open System.Linq

module BitMap =
    type bitmap = System.Collections.BitArray
    let init (bools:bool []) = new System.Collections.BitArray(bools)

    let isSet index (b:bitmap) =
        b.Get(index)

    let countUpto (uptoIndex:int) (b:bitmap) = 
        let mutable count = 0
        for i=0 to uptoIndex-1 do
            if b.Get(i) then
                count <- count+1
        count

module PSeqOrdered =
    let map (mapping:'a->'b) (source:seq<'a>) =
        source.AsParallel().AsOrdered().Select(mapping)

module SimpleReadWrite = // 
    let writeValue (writer:BinaryWriter) (value:int option) =
        match value with
        | Some(v) -> writer.Write(v)
        | None -> writer.Write(Int32.MinValue)

    let writeValues fileName (values:int option [][]) =
        use writer = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
        values
        |> Seq.concat
        |> Seq.iter (writeValue writer)
        writer.Write(1.250F);
    
    let readValue (reader:BinaryReader) cellIndex = 
        reader.BaseStream.Seek(cellIndex*4L,SeekOrigin.Begin) |> ignore
        let value = reader.ReadInt32()
        if value <> Int32.MinValue then
            Some(value) 
        else 
            None
        
    let readValues fileName indices = 
        use reader = new BinaryReader(File.Open(fileName, FileMode.Open))
        let readValueFromReader = readValue reader
        // Performance IDEA: sort the indices
        Seq.map readValueFromReader indices

module AsciiToBin =

    let parseValue nodata (v:string) =
        if v <> nodata then
            Some(int v)
        else
            None

    let parseRow nodata (values:string []) =
        let parsed = values |> Array.map (parseValue nodata)
        let bitmap = parsed |> Array.map Option.isSome |> BitMap.init
        //let somes = parsed |> Array.filter Option.isSome |> Array.map (fun x -> x.Value)
        (bitmap, parsed)



    let loadAscii path =
        let lines = File.ReadLines(path) 
        let isHeader (l:string) = (l.Length < 1000)
        let header = lines.TakeWhile(isHeader).ToDictionary((fun (l:string) -> l.Split([|' '|], StringSplitOptions.RemoveEmptyEntries).[0]), (fun (l:string) -> l.TrimEnd([|'\n'|]).Split([|' '|]).Last()))
        let mutable mnodata = ""
        if not (header.TryGetValue("NODATA_value", &(mnodata))) then 
            mnodata <- "-99999"
        let nodata = mnodata

        let inline splitLine (x:string) = x.Trim([|' '; '\n'|]).Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        let values = lines.SkipWhile(isHeader)

        let bitmap, values = 
            values
            |> PSeqOrdered.map (splitLine >> (parseRow nodata)) // PSeq changes order of the sequence
            |> Array.ofSeq
            |> Array.unzip
        bitmap, values

module Test =
    let writeValues = 
        AsciiToBin.writeValues @"D:\temp\write_cells.bin" [[Option(1);Option(2);None]]
        
        
        