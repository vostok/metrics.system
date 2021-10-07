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