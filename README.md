Mechanical v3
=============

General Info
------------

### Why another library?
To ease many everyday tasks, like checking preconditions, saving data in a structured manner, writing cross-platform code, and countless others.

### Building
The libraries are written using Visual Studio 2015 Community.

### License
MIT (unless otherwise stated)

Projects
--------
* **Mechanical3.Portable:** the core, platform-independent library
* **Mechanical3.NET45:** some additional functionality that requires the .NET Framework 4.5
* **Mechanical3.Tests:** unit tests for both of the above
* **Mechanical3.IncrBuildNum:** a small tool for increasing a build number, independent of assembly attributes
* **Mechanical3.ScriptEditor:** an experimental library that enables basic scripting using the Roslyn compiler.

Main Mechanical3 namespaces
---------------------------

### Core
Features most commonly used when coding. (Helps working with IDisposable, adding data to exceptions, logging, string pattern matching, ... etc.)

### Misc
Utilities used by all other namespaces. Helps working with IDisposable, enums, string and substrings, ... etc.

### DataStores
Manual, low-level serialization:
* separation of file format (e.g. XML, JSON) and data format (e.g. DateTime formatting)
* Hierarchical (unlike Text/BinaryWriter)

### IO.FileSystems
Abstract interfaces for the most common file system functionality.
* Useful when writing cross-platform library code.
* File (or directory) paths are converted into a platform independent string, which is hidden behind the FilePath class. This means that they can be saved on one platform, and loaded on another without problems.
* Only the most common operations are currently supported.

### Events
Event Queue pattern with a twist (also known as event aggregator, message queue, ... etc.)
* Strongly typed: for example you can register for all events implementing a specific interface
* You can enqueue events in a fire & forget manner, similar to the usual implementation...
* ... but through tasks you can also be notified when it was handled, and if you wish, you may even intercept any exceptions thrown.
* This is similar to the functionality of standard .NET events.
* Events are either processed manually (ManualEventPump), or using a single long-running task (TaskEventQueue)
* A two-stage shutdown process allows all subscribers plenty of opportunities to clean up after themselves.

### Loggers
A basic logging implementation.
* Simple but effective interface. A subset of NLog, but has no dependency on it.
* Designed to be simple to replace with your own logger, should you choose to do so.
* Log entries of the default implementation can be serialized using the data store.
* Exceptions can be serialized, and all major information can be restored, even on different platforms, which do not have the exception type (e.g. Java exceptions saved on Android can be viewed on Windows).

### MVVM
Few minor tools for UI development. Useful for quickly developing small apps.
* INotifyPropertyChanged implementation using the caller info attribute.
* UI thread handling. (Helps writing platform independent view model libraries).
* A simple delegate based ICommand implementation.
* A simple base class for quickly implementing IValueConverter using templates.
* PropertyChangedActions class allows registering INotifyPropertyChanged listeners, either directly or using a property "chain" (e.g. if the "Instance.Property0.Property1.Property2" property changes, invoke this delegate)