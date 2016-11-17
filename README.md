# PoshTasks

A parallel processing abstract cmdlet base class to help increase performance of PowerShell binary cmdlets.

Install nuget package and inherit from TaskCmdlet.
Implement ProcessTask function - this is where the cmdlet processing occurs (ie what would once be in ProcessRecord override).
Override PostProcessTask, CreateProcessTask if necessary. All other cmdlet functions may be overwritten as necessary. One major caveat is to *not* use WriteObject, WriteError etc outside of ProcessRecord and PostProcessRecord. A cmdlet can only write to the console on the parent thread, you will raise an exception if you attempt to do this on a new thread.

See Sample\GetRemoteService.cs for an example.
