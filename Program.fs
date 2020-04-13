open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

[<MemoryDiagnoser>]
type SceneBench() =
    [<Benchmark(Baseline=true)>]
    member _.Original() = Original.runScene [||] |> ignore

    [<Benchmark>]
    member _.Modified() = Modified.runScene [||] |> ignore

[<MemoryDiagnoser>]
type RenderBench() =
    let originalScene = Original.runScene [||]
    let modifiedScene = Modified.runScene [||]

    [<Benchmark(Baseline=true)>]
    member _.Original() = Original.runRender [||] originalScene |> ignore

    [<Benchmark>]
    member _.Modified() = Modified.runRender [||] modifiedScene |> ignore

[<EntryPoint>]
let main _ =
    let summary = BenchmarkRunner.Run<SceneBench>()
    printfn "%A" summary

    let summary = BenchmarkRunner.Run<RenderBench>()
    printfn "%A" summary
    0
