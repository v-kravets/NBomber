module internal NBomber.DomainServices.TestHost.TestHostPlugins

open FSharp.Control.Tasks.NonAffine
open FsToolkit.ErrorHandling

open NBomber
open NBomber.Contracts
open NBomber.Errors
open NBomber.Extensions.InternalExtensions
open NBomber.Infra.Dependency

let initPlugins (dep: IGlobalDependency) (context: IBaseContext) = taskResult {
    try
        for plugin in dep.WorkerPlugins do
            dep.Logger.Information("Start init plugin: '{PluginName}'.", plugin.PluginName)
            do! plugin.Init(context, dep.InfraConfig |> Option.defaultValue Constants.EmptyInfraConfig)
    with
    | ex -> return! AppError.createResult(InitScenarioError ex)
}

let startPlugins (dep: IGlobalDependency) = task {
    for plugin in dep.WorkerPlugins do
        try
            plugin.Start() |> ignore
        with
        | ex -> dep.Logger.Warning(ex, "Failed to start plugin '{PluginName}'.", plugin.PluginName)
}

let stopPlugins (dep: IGlobalDependency) = task {
    for plugin in dep.WorkerPlugins do
        try
            dep.Logger.Information("Stop plugin: '{PluginName}'.", plugin.PluginName)
            do! plugin.Stop()
        with
        | ex -> dep.Logger.Warning(ex, "Stop plugin '{PluginName}' failed.", plugin.PluginName)
}
