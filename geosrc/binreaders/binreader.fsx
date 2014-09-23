open System
open System.IO

let readValue (reader:BinaryReader) cellIndex = 
    // set stream to correct location
    reader.BaseStream.Seek(int64 (cellIndex*4), SeekOrigin.Begin) |> ignore
    match reader.ReadInt32() with
    | Int32.MinValue -> None
    | v -> Some(v)
        
let readValues indices fileName = 
    use reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
    // Use list or array to force creation of values (otherwise reader gets disposed before the values are read)
    let values = Array.map (readValue reader) indices
    values

// sequential reading from disk
let readValuesSeq indices fileName =
    use reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
    let max = Seq.max indices
    let min = Seq.min indices
    reader.BaseStream.Seek(int64 (min*4), SeekOrigin.Begin) |> ignore
    let bytes = reader.ReadBytes((max-min)*4)
    let values = indices |> Array.map (fun i -> (match (Convert.ToInt32(bytes.[i-min])) with | Int32.MinValue -> None | v -> Some(v)))
    values

let getindices n random = 
    if random then
        let r = new Random(1000)
        Array.init n (fun i -> (r.Next(0, 2332799))) |> Array.sort
    else
        Array.init n (fun i -> 10000+(i*3))

let allmarspec reader random outer inner = 
    let dir = "D:\\temp\\sbg_10m\\"
    let paths = Directory.GetFiles(dir)
    let indices = getindices inner random
    Array.create outer [|1l|] 
    |> Array.map (fun i -> Array.map (reader indices) paths)
    |> fun arr-> arr.[0]

let time f x =
    let sw = Diagnostics.Stopwatch.StartNew()
    let fx = f x
    printf "Execution time: %fs %i %i\n" (float sw.ElapsedMilliseconds / 1000.0) (Array.length fx) (Array.length (fx.[0]))

// all the below read data from 39 marspec 10m files (2160*1080 cells)

(allmarspec readValues false 1 10);;
time (allmarspec readValues false 10) 10;; (* <0.05s *)
time (allmarspec readValues false 100) 100;; (* <0.8s  *)
time (allmarspec readValues false 1000) 100;; (* <7.5s *)
time (allmarspec readValues false 10) 10000;; (* <6s *)
time (allmarspec readValues false 1) 100000;; (* <6s *)
let long_running () =
    time (allmarspec readValues false 10000) 10;; (* <20s *)

// random indices
time (allmarspec readValues true 10) 10;; (* <0.05s *)
time (allmarspec readValues true 100) 100;; (* <2.5s  *)
time (allmarspec readValues true 1000) 100;; (* <23s *)
time (allmarspec readValues true 10) 10000;; (* <9s *)
time (allmarspec readValues true 1) 100000;; (* <7s *)
let long_running_random () =
    time (allmarspec readValues true 10000) 10;; (* <42s *)

 // read data from disk sequentially in bulk
(allmarspec readValuesSeq false 1 10);;
time (allmarspec readValuesSeq false 10) 10;; (* <0.03s *)
time (allmarspec readValuesSeq false 100) 100;; (* <0.3s  *)
time (allmarspec readValuesSeq false 1000) 100;; (* <2.5s *)
time (allmarspec readValuesSeq false 10) 10000;; (* <0.5s *)
time (allmarspec readValuesSeq false 1) 100000;; (* <0.5s *)
time (allmarspec readValuesSeq false 1) 1000000;; (* <7s *)

// random indices
time (allmarspec readValuesSeq true 10) 10;; (* <3s *)
time (allmarspec readValuesSeq true 100) 100;; (* <27s  *)
time (allmarspec readValuesSeq true 1000) 100;; (* <270s *)
time (allmarspec readValuesSeq true 10) 10000;; (* <4s *)
time (allmarspec readValuesSeq true 1) 100000;; (* <1s *)
time (allmarspec readValuesSeq true 1) 1000000;; (* <7s *)
