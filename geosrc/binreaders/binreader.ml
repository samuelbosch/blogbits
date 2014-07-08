(* OCaml version *)

(* read little endian integers from the input channel *)
let input_le_int32 inchannel = (* http://stackoverflow.com/a/6031286/477367 *)
  let res = ref 0l in
    for i = 0 to 3 do
      let byte = input_byte inchannel in
        res := Int32.logor !res (Int32.shift_left (Int32.of_int byte) (8*i))
    done;

    match !res with
      | -2147483648l -> None
      | v -> Some(v)

let readvalue inchannel index =
  seek_in inchannel (index*4);
  input_le_int32 inchannel

let readvalues (indices:int array) filename =
  let inchannel = open_in_bin filename in
    try
      let result = Array.map (readvalue inchannel) indices in
        close_in inchannel;
        result
    with e ->
      close_in_noerr inchannel;
      raise e

let getindices n = 
  Array.init n (fun i -> 10000+(i*3))

let allmarspec outer inner = 
  let dir = "D:\\temp\\sbg_10m\\" in
  let paths = Sys.readdir dir |> Array.map (fun p -> String.concat "" [dir; p]) in
  let indices = getindices inner in
    Array.make outer [|1l|] 
    |> Array.map (fun i -> Array.map (readvalues indices) paths)
    |> fun arr-> arr.(0)



(* time your code and drop the result, 
   based on http://stackoverflow.com/a/9061574/477367 *)
let time f x =
  let t = Sys.time() in
  let fx = f x in
    Printf.sprintf "Execution time: %fs %i %i\n" (Sys.time() -. t) (Array.length fx) (Array.length (fx.(0)));;

allmarspec 1 10;;
time (allmarspec 10) 10;; (* <0.07s *)
time (allmarspec 100) 100;; (* <0.6s  *)
time (allmarspec 1000) 100;; (* <6s *)
time (allmarspec 10) 10000;; (* <2.3s *)
time (allmarspec 1) 100000;; (* <2.1s *)

let long_running () =
  time (allmarspec 10000) 10;; (* <48s *)
