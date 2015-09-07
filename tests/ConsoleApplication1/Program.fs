open Microsoft.FSharp.Compiler.SourceCodeServices
open System.IO
open System.Diagnostics
open System

let sysLib nm = 
    if System.Environment.OSVersion.Platform = System.PlatformID.Win32NT then // file references only valid on Windows 
        @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\" + nm + ".dll"
    else
        let sysDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
        let (++) a b = System.IO.Path.Combine(a,b)
        sysDir ++ nm + ".dll"

let fsCore4300() = 
    if System.Environment.OSVersion.Platform = System.PlatformID.Win32NT then // file references only valid on Windows 
        @"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0\FSharp.Core.dll"  
    else 
        sysLib "FSharp.Core"

let mkProjectCommandLineArgs (dllName, fileNames) = 
    [|  yield "--simpleresolution" 
        yield "--noframework" 
        yield "--debug:full" 
        yield "--define:DEBUG" 
        yield "--optimize-" 
        yield "--out:" + dllName
        yield "--doc:test.xml" 
        yield "--warn:3" 
        yield "--fullpaths" 
        yield "--flaterrors" 
        yield "--target:library" 
        for x in fileNames do 
            yield x
        let references = 
            [ yield sysLib "mscorlib"
              yield sysLib "System"
              yield sysLib "System.Core"
              yield fsCore4300() ]
        for r in references do
                yield "-r:" + r |]

let checker = FSharpChecker.Create()

module Project1 = 
    open System.IO

    let fileNamesI = [ for i in 1 .. 10 -> (i, Path.ChangeExtension(Path.GetTempFileName(), ".fs")) ]
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSources = [ for (i,f) in fileNamesI -> (f, "module M" + string i) ]
    for (f,text) in fileSources do File.WriteAllText(f, text)
    let fileSources2 = [ for (i,f) in fileSources -> f ]

    let fileNames = [ for (_,f) in fileNamesI -> f ]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)

let rec getFilesRecursively dir =
    seq {
        yield! Directory.EnumerateFiles dir
        for dir in Directory.EnumerateDirectories dir do
            yield! getFilesRecursively dir
    }

[<EntryPoint>]
let main _ = 
//    let files = 
//        getFilesRecursively @"D:\github\FSharp.Compiler.Service\src\fsharp" 
//        |> Seq.filter (fun f -> Path.GetExtension f = ".fs")
//        |> Seq.map (fun f -> f, File.ReadAllText f)
//        |> Seq.toArray
//    printfn "%d files, %d MB\nPress any key to start parsing..." files.Length ((files |> Array.sumBy (fun (_, src) -> src.Length)) / 1024 / 1024)
//    Console.ReadKey() |> ignore
    let sw = Stopwatch.StartNew()
    
    for fileName, fileSource in files do
        checker.ParseFileInProject(fileName, fileSource, Project1.options) |> Async.RunSynchronously |> ignore

    printfn "Done in %O" sw.Elapsed
    Console.ReadKey() |> ignore
    0
