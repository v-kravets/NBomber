﻿namespace NBomber.CSharp

#nowarn "3211"

open System
open System.Threading.Tasks
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open Serilog

open NBomber
open NBomber.Contracts
open NBomber.Configuration

type ClientFactory =

    static member Create
        (name: string,
         initClient: Func<int,IBaseContext,Task<'TClient>>,
         [<Optional;DefaultParameterValue(null:obj)>] disposeClient: Func<'TClient,IBaseContext,Task>,
         [<Optional;DefaultParameterValue(Constants.DefaultClientCount:int)>] clientCount: int) =

        let defaultDispose = (fun (client,context) ->
            match client :> obj with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()
            Task.CompletedTask
        )

        let dispose =
            if isNull(disposeClient :> obj) then defaultDispose
            else disposeClient.Invoke

        NBomber.Domain.ClientPool.ClientFactory(name, clientCount, initClient.Invoke, dispose)
        :> IClientFactory<'TClient>

/// Data feed helps you to inject dynamic data into your test.
type Feed =

    /// Creates Feed that picks constant value per Step copy.
    /// Every Step copy will have unique constant value.
    static member CreateConstant (name, data: 'T seq) =
        NBomber.Domain.Feed.constant(name, data)

    /// Creates Feed that picks constant value per Step copy.
    /// Every Step copy will have unique constant value.
    static member CreateConstant (name, getData: Func<'T seq>) =
        NBomber.Domain.Feed.constant(name, getData.Invoke())

    /// Creates Feed that randomly picks an item per Step invocation.
    static member CreateCircular (name, data: 'T seq) =
        NBomber.Domain.Feed.circular(name, data)

    /// Creates Feed that randomly picks an item per Step invocation.
    static member CreateCircular (name, getData: Func<'T seq>) =
        NBomber.Domain.Feed.circular(name, getData.Invoke())

    /// Creates Feed that returns values from  value on every Step invocation.
    static member CreateRandom (name, data: 'T seq) =
        NBomber.Domain.Feed.random(name, data)

    /// Creates Feed that returns values from  value on every Step invocation.
    static member CreateRandom (name, getData: Func<'T seq>) =
        NBomber.Domain.Feed.random(name, getData.Invoke())

type Step =

    static member Create
        (name: string,
         clientFactory: IClientFactory<'TClient>,
         feed: IFeed<'TFeedItem>,
         exec: Func<IStepContext<'TClient,'TFeedItem>,Task<Response>>,
         [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>] doNotTrack: bool) =

        FSharp.Step.create(name, exec.Invoke, clientFactory, feed, doNotTrack)

    static member Create
        (name: string,
         clientFactory: IClientFactory<'TClient>,
         exec: Func<IStepContext<'TClient,unit>,Task<Response>>,
         [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>] doNotTrack: bool) =

        FSharp.Step.create(name, exec.Invoke, clientFactory, doNotTrack = doNotTrack)

    static member Create
        (name: string,
         feed: IFeed<'TFeedItem>,
         exec: Func<IStepContext<unit,'TFeedItem>,Task<Response>>,
         [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>] doNotTrack: bool) =

        FSharp.Step.create(name, exec.Invoke, feed = feed, doNotTrack = doNotTrack)

    static member Create
        (name: string,
         exec: Func<IStepContext<unit,unit>,Task<Response>>,
         [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>] doNotTrack: bool) =

        FSharp.Step.create(name, exec.Invoke, doNotTrack = doNotTrack)

    /// Creates pause step with specified duration.
    static member CreatePause(duration: TimeSpan) =
        FSharp.Step.createPause(duration)

    /// Creates pause step with specified duration in milliseconds.
    static member CreatePause(milliseconds: int) =
        FSharp.Step.createPause(milliseconds)

    /// Creates pause step with specified duration in lazy mode.
    /// It's useful when you want to fetch value from some configuration.
    static member CreatePause(getDuration: Func<TimeSpan>) =
        FSharp.Step.createPause(getDuration.Invoke)

    /// Creates pause step in milliseconds in lazy mode.
    /// It's useful when you want to fetch value from some configuration.
    static member CreatePause(getDuration: Func<int>) =
        FSharp.Step.createPause(getDuration.Invoke)

[<Extension>]
type ScenarioBuilder =

    /// Creates scenario with steps which will be executed sequentially.
    static member CreateScenario(name: string, [<ParamArray>]steps: IStep[]) =
        FSharp.Scenario.create name (Seq.toList steps)

    /// Initializes scenario.
    /// You can use it to for example to prepare your target system or to parse and apply configuration.
    [<Extension>]
    static member WithInit(scenario: Scenario, initFunc: Func<IScenarioContext,Task>) =
        { scenario with Init = Some initFunc.Invoke }

    /// Cleans scenario's resources.
    [<Extension>]
    static member WithClean(scenario: Scenario, cleanFunc: Func<IScenarioContext,Task>) =
        { scenario with Clean = Some cleanFunc.Invoke }

    /// Sets warm-up duration
    /// Warm-up will just simply start a scenario with a specified duration.
    [<Extension>]
    static member WithWarmUpDuration(scenario: Scenario, duration: TimeSpan) =
        scenario |> FSharp.Scenario.withWarmUpDuration(duration)

    [<Extension>]
    static member WithoutWarmUp(scenario: Scenario) =
        scenario |> FSharp.Scenario.withoutWarmUp

    /// Sets load simulations.
    /// Default value is: InjectPerSec(rate = 50, during = minutes 1)
    [<Extension>]
    static member WithLoadSimulations (scenario: Scenario, [<ParamArray>]loadSimulations: LoadSimulation[]) =
        scenario |> FSharp.Scenario.withLoadSimulations(Seq.toList loadSimulations)

    /// Sets custom steps order that will be used by NBomber Scenario executor.
    /// By default, all steps are executing sequentially but you can inject your custom order.
    /// getStepsOrder function will be invoked on every turn before steps list execution.
    [<Extension>]
    static member WithCustomStepsOrder (scenario: Scenario, getStepsOrder: Func<int[]>) =
        scenario |> FSharp.Scenario.withCustomStepsOrder(getStepsOrder.Invoke)

[<Extension>]
type NBomberRunner =

    /// Registers scenarios in NBomber environment.
    /// Scenarios will be run in parallel.
    static member RegisterScenarios([<ParamArray>]scenarios: Scenario[]) =
        scenarios |> Seq.toList |> FSharp.NBomberRunner.registerScenarios

    /// Sets test suite name
    /// Default value is: nbomber_default_test_suite_name.
    [<Extension>]
    static member WithTestSuite(context: NBomberContext, testSuite: string) =
        context |> FSharp.NBomberRunner.withTestSuite(testSuite)

    /// Sets test name
    /// Default value is: nbomber_default_test_name.
    [<Extension>]
    static member WithTestName(context: NBomberContext, testName: string) =
        context |> FSharp.NBomberRunner.withTestName(testName)

    /// Sets output report name.
    /// Default name: nbomber_report.
    [<Extension>]
    static member WithReportFileName(context: NBomberContext, reportFileName: string) =
        context |> FSharp.NBomberRunner.withReportFileName(reportFileName)

    /// Sets output report folder path.
    /// Default folder path: "./reports".
    [<Extension>]
    static member WithReportFolder(context: NBomberContext, reportFolderPath: string) =
        context |> FSharp.NBomberRunner.withReportFolder(reportFolderPath)

    [<Extension>]
    static member WithReportFormats(context: NBomberContext, [<ParamArray>]reportFormats: ReportFormat[]) =
        let formats = reportFormats |> Seq.toList
        context |> FSharp.NBomberRunner.withReportFormats(formats)

    /// Sets to run without reports
    [<Extension>]
    static member WithoutReports(context: NBomberContext) =
        context |> FSharp.NBomberRunner.withoutReports

    /// Sets real-time reporting interval.
    /// Default value: 10 seconds, min value: 5 sec
    [<Extension>]
    static member WithReportingInterval(context: NBomberContext, interval: TimeSpan) =
        context |> FSharp.NBomberRunner.withReportingInterval interval

    /// Sets reporting sinks.
    /// Reporting sink is used to save real-time metrics to correspond database.
    [<Extension>]
    static member WithReportingSinks(context: NBomberContext, reportingSinks: IReportingSink[]) =
        let sinks = reportingSinks |> Seq.toList
        context |> FSharp.NBomberRunner.withReportingSinks sinks

    /// Sets worker plugins.
    /// Worker plugin is a plugin that starts at the test start and works as a background worker.
    [<Extension>]
    static member WithWorkerPlugins(context: NBomberContext, [<ParamArray>]plugins: IWorkerPlugin[]) =
        let pluginsList = plugins |> Seq.toList
        context |> FSharp.NBomberRunner.withWorkerPlugins(pluginsList)

    /// Loads configuration.
    /// The following formats are supported:
    /// - json (.json)
    [<Extension>]
    static member LoadConfig(context: NBomberContext, path: string) =
        context |> FSharp.NBomberRunner.loadConfig(path)

    /// Loads infrastructure configuration.
    /// The following formats are supported:
    /// - json (.json)
    [<Extension>]
    static member LoadInfraConfig(context: NBomberContext, path: string) =
        context |> FSharp.NBomberRunner.loadInfraConfig(path)

    /// Sets logger configuration.
    /// Make sure that you always return a new instance of LoggerConfiguration.
    /// You can also configure logger via configuration file.
    /// For this use NBomberRunner.loadInfraConfig
    [<Extension>]
    static member WithLoggerConfig(context: NBomberContext, buildLoggerConfig: Func<LoggerConfiguration>) =
        context |> FSharp.NBomberRunner.withLoggerConfig(buildLoggerConfig.Invoke)

    /// Sets application type.
    /// The following application types are supported:
    /// - Console: is suitable for interactive session (will display progress bar)
    /// - Process: is suitable for running tests under test runners (progress bar will not be shown)
    /// By default NBomber will automatically identify your environment: Process or Console.
    [<Extension>]
    static member WithApplicationType(context: NBomberContext, applicationType: ApplicationType) =
        context |> FSharp.NBomberRunner.withApplicationType(applicationType)

    /// Disables hints analyzer.
    /// Hints analyzer - analyze node stats to provide some hints in case of finding wrong usage or some other issue.
    [<Extension>]
    static member DisableHintsAnalyzer(context: NBomberContext) =
        context |> FSharp.NBomberRunner.disableHintsAnalyzer

    [<Extension>]
    static member Run(context: NBomberContext) =
        match FSharp.NBomberRunner.run context with
        | Ok stats  -> stats
        | Error msg -> failwith msg

    /// Runs scenarios with arguments.
    /// The following CLI commands are supported:
    /// -c or --config: loads configuration,
    /// -i or --infra: loads infrastructure configuration.
    /// Examples of possible args:
    /// -c config.json -i infra_config.json
    /// --config=config.json --infra=infra_config.json
    [<Extension>]
    static member Run(context: NBomberContext, [<ParamArray>]args: string[]) =
        match FSharp.NBomberRunner.runWithArgs args context with
        | Ok stats  -> stats
        | Error msg -> failwith msg

type Simulation =

    /// Injects a given number of scenario copies with a linear ramp over a given duration. Use it for ramp up and rump down.
    static member RampConstant(copies: int, during: TimeSpan) =
        LoadSimulation.RampConstant(copies, during)

    /// Injects a given number of scenario copies at once and keep them running, during a given duration.
    static member KeepConstant(copies: int, during: TimeSpan) =
        LoadSimulation.KeepConstant(copies, during)

    /// Injects a given number of scenario copies from the current rate to target rate, defined in scenarios per second, during a given duration.
    static member RampPerSec(rate: int, during: TimeSpan) =
        LoadSimulation.RampPerSec(rate, during)

    /// Injects a given number of scenario copies at a constant rate, defined in scenarios per second, during a given duration.
    static member InjectPerSec(rate: int, during: TimeSpan) =
        LoadSimulation.InjectPerSec(rate, during)

    /// Injects a given number of scenario copies at a random rate, defined in scenarios per second, during a given duration.
    static member InjectPerSecRandom(minRate:int, maxRate:int, during:TimeSpan) =
        LoadSimulation.InjectPerSecRandom(minRate, maxRate, during)

namespace NBomber.CSharp.SyncApi

    open System
    open System.Runtime.InteropServices

    open NBomber
    open NBomber.Contracts
    open NBomber.FSharp.SyncApi

    type SyncStep =

        static member Create
            (name: string,
             pool: IClientFactory<'TClient>,
             feed: IFeed<'TFeedItem>,
             exec: Func<IStepContext<'TClient,'TFeedItem>,Response>,
             [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>]doNotTrack: bool) =

            SyncStep.create(name, exec.Invoke, pool, feed, doNotTrack)

        static member Create
            (name: string,
             pool: IClientFactory<'TClient>,
             exec: Func<IStepContext<'TClient,unit>,Response>,
             [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>]doNotTrack: bool) =

            SyncStep.create(name, exec.Invoke, pool, doNotTrack = doNotTrack)

        static member Create
            (name: string,
             feed: IFeed<'TFeedItem>,
             exec: Func<IStepContext<unit,'TFeedItem>,Response>,
             [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>]doNotTrack: bool) =

            SyncStep.create(name, exec.Invoke, feed = feed, doNotTrack = doNotTrack)

        static member Create
            (name: string,
             exec: Func<IStepContext<unit,unit>,Response>,
             [<Optional;DefaultParameterValue(Constants.DefaultDoNotTrack:bool)>]doNotTrack: bool) =

            SyncStep.create(name, exec.Invoke, doNotTrack = doNotTrack)
