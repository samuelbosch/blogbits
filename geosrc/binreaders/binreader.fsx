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

let getindices n = 
    Array.init n (fun i -> 10000+(i*3))

let allmarspec outer inner = 
    let dir = "D:\\temp\\sbg_10m\\"
    let paths = Directory.GetFiles(dir)
    let indices = getindices inner
    Array.create outer [|1l|] 
    |> Array.map (fun i -> Array.map (readValues indices) paths)
    |> fun arr-> arr.[0]

let time f x =
    let sw = Diagnostics.Stopwatch.StartNew()
    let fx = f x
    printf "Execution time: %fs %i %i\n" (float sw.ElapsedMilliseconds / 1000.0) (Array.length fx) (Array.length (fx.[0]))

(allmarspec 1 10);;
time (allmarspec 10) 10;; (* <0.05s *)
time (allmarspec 100) 100;; (* <0.8s  *)
time (allmarspec 1000) 100;; (* <7.5s *)
time (allmarspec 10) 10000;; (* <6s *)
time (allmarspec 1) 100000;; (* <6s *)
let long_running () =
    time (allmarspec 10000) 10;; (* <20s *)