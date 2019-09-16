# EventLogForwarder

This component is part of the (event-log BOSH release)[github.com/cloudfoundry-incubator/event-log-release].

### building

On a windows machine, with this repo as your current directory:

```
nuget.exe restore
dotnet.exe build
```

### testing

On a windows machine, with this repo as your current directory:

```
.\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe .\Forwarder.Tests\bin\Debug\Forwarder.Tests.dll
.\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe .\Tailer.Tests\bin\Debug\Tailer.Tests.dll
```

Successful test output for one of the above commands should look like:
```
xUnit.net Console Runner (64-bit .NET 4.0.30319.42000)
  Discovering: Forwarder.Tests
  Discovered:  Forwarder.Tests
  Starting:    Forwarder.Tests
  Finished:    Forwarder.Tests
=== TEST EXECUTION SUMMARY ===
   Forwarder.Tests  Total: 23, Errors: 0, Failed: 0, Skipped: 0, Time: 30.926s
```
