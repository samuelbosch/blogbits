package main

import (
    "fmt"
    "os"
    "syscall"
    "io/ioutil"
    "path"
    "time"
)

func bytes2int(b []byte) int32 {
    v := int32(0)
    for  i := 0; i < 4; i++ {
        v = v | (int32(b[i]) << (uint(8*i)))
    }
    return v
}

func readValues(indices []int, filename string) []int32 {
    results := make([]int32, len(indices))
    b := make([]byte, 4)
    f,_ := os.Open(filename)
    fd := syscall.Handle(f.Fd())
    //value := int32(0)
    for i, cellIndex := range indices {
        syscall.Seek(fd, int64(cellIndex*4), os.SEEK_SET) // f.Seek(int64(cellIndex*4), os.SEEK_SET)
        syscall.Read(fd, b) //f.Read(b)
        value := bytes2int(b) // around 10-20% faster then binary.Read
        //package "encoding/binary"
        //binary.Read(f,binary.LittleEndian, &value)
        if value != -2147483648 {
            results[i] = value
        } else {
            results[i] = 99999
        }
    }
    return results
}

func getindices(n int) []int {
    indices := make([]int, n)
    for i := range indices {
        indices[i] = 10000+(i*3)
    }
    return indices
}

func allmarspec (outer int, inner int) []int32 {
    dir := path.Join("D:","temp","sbg_10m")
    files,_ := ioutil.ReadDir(dir)
    indices := getindices(inner)
    results := make([]int32, len(files))
    start := time.Now()
    for i := 0; i<outer;i++ {
        for fi,fileInfo := range files {
            results[fi] = readValues(indices, path.Join(dir, fileInfo.Name()))[0]
        }
    }
    fmt.Printf("allmarspec %d %d took %s\n" ,outer, inner,time.Now().Sub(start).String())
    return results
}

func main() {
    //fmt.Println(readValues([]int{0,1,2,13}, "D:\\temp\\sbg_10m\\bathy_10m.sbg"))
    
    //fmt.Println(allmarspec(100,100)) // 1s
    //fmt.Println(allmarspec(1000,100)) // 10s
    fmt.Println(allmarspec(10,10000)) // 8s
    //fmt.Println(allmarspec(1,100000)) // 8s
    //fmt.Println(allmarspec(10000,10)) // 25s
}