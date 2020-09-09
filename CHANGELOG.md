## 0.2.2 (09-09-2020):

Added `CpuTotal` field.

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