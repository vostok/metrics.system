## 0.3.24 (16-12-2024): 

Bump NuGet deps versions

## 0.3.23 (02-05-2024):

Performance improvements for linux system metrics collectors for large hosts. CPU consumption goes down (significantly), GC time % goes up (mildly).

It is possible to switch back to previous metric collecting logic via environment variable VOSTOK_METRICS_SYSTEM_LINUX_USE_LEGACY_COLLECTOR=True.

## 0.3.21 (08-04-2024):

Do not log warnings about disabled network interfaces on newer Linux kernels.

## 0.3.20 (22-09-2023):

Return back changes from 0.3.18 using RuntimeDetector to fix "Overlapped I/O operation is in progress" exception for framework 4.7.2/4.8.

## 0.3.19 (19-07-2023):

Revert `0.3.18` changes due to problems with `GetProcessorCount`

## 0.3.18 (26-06-2023):

Fixed incorrect usage of Environment.ProcessorCount value (use host cores instead).

[Backward compability was broken in .net6](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/environment-processorcount-on-windows)

## 0.3.17 (28-09-2022):

Fixed unlimited cgroup limits.

## 0.3.16 (23-08-2022):

Application limits can now be obtained from cgroup when present. 

## 0.3.15 (23-08-2022):

Apply common helpers and counters from `Vostok.Commons.Helpers`.

## 0.3.14 (15-06-2022):

Extended useful Linux network interfaces list for proper network usage metric collection.

## 0.3.13 (13-04-2022):

Fixed NRE on `HostMetrics` reporting

## 0.3.12 (02-03-2022):

Allow multiple dispose calls of `HostMonitor` and `CurrentProcessMonitor`.

## 0.3.11 (26-01-2022):

Add nullable timespan to GC monitor extension

## 0.3.10 (26-01-2022):

Added extensions to GC Monitor for overriding default metrics reporting period.

## 0.3.9 (10-12-2021):

Added nullable cpu & memory limit properties.

## 0.3.8 (06-12-2021):

Added `net6.0` target.

## 0.3.6 (23-11-2021):

Fixed `net6.0` compability (#21).

## 0.3.4 (15-11-2021):

Make host cpu collection OS specific since `Environment.ProcessorCount` depends on the process.

## 0.3.3 (07-10-2021):

Made `CurrentProcessMonitor` and `HostMonitor` `IDisposable`. 
Methods like `ObserveMetrics` now return `IDisposable`.

## 0.3.2 (22-09-2021):

Fixed a couple of bugs:
- Teaming collector doesn't panic now when file descriptors count is larger than 1024
- Collect file descriptors count on linux instead of allocated size

## 0.3.1 (09-09-2021):

Removed obsolete files

## 0.3.0 (09-09-2021):

Added DNS metrics:
- DNS lookups count
- Summary of DNS lookup time 

Added sockets metrics:
- TCP connections count
- UDP connections count

## 0.2.11 (02-08-2021):

Added disk number in order to collect usage info from devices with Xen type. 

## 0.2.10 (25-03-2021):

Added support for `team` interfaces. 

## 0.2.9 (02-02-2021):

Added RootDirectory to DiskSpaceInfo.cs
Disabled `team` interfaces in NetworkUtilizationCollector_Linux due to absence of `speed` file in old kernels.
Fixed a couple of bugs in NetworkUtilizationCollector_Linux.

## 0.2.8 (22-10-2020):

CurrentProcessMetricsCollector: fixed consumed CPU cores metrics inside Linux containers.

## 0.2.7 (10-10-2020):

Fixed a bug in disk traffic metrics calculation (per second metrics weren't appropriately scaled).

## 0.2.6 (05-10-2020):

- CurrentProcessMetrics: added limit metrics
- CurrentProcessMetrics: added limit utilization metrics
- CurrentProcessMetrics: added process uptime metric
- HostMetricsCollector: added options enable/disable collection of different metrics

## 0.2.5 (21-09-2020):

Added per-network-interface metrics.

## 0.2.4 (10-09-2020):

Added `CpuTotalCores` field.

## 0.2.2 (24-08-2020):

- Added network usage metrics.
- Added disk usage metrics.
- Added Page faults / sec metrics.

## 0.2.1 (07-08-2020):

- DiskSpaceCollector: only update mounts once per call, not once per disk.
- DiskSpaceCollector: replace slashes in disk names with dashes on Linux.
- DiskSpaceCollector: removed annoying repeated warns about duplicate disks on Linux.

## 0.2.0 (07-08-2020):

Added host metrics collection (CPU, Memory, Disk and TCP connections).

## 0.1.3 (17-07-2020):

Fixed Int32 overflow in process memory metrics on Linux.

## 0.1.2 (09-07-2020):

Fixed a bug in current process metrics reporting to Vostok metric context.

## 0.1.1 (09-07-2020):

Metric-related extensions were moved from CurrentProcessMonitor to CurrrentProcessMetricsCollector.

## 0.1.0 (09-07-2020):

Initial release.
