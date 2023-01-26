# MAGIC Build Pipeline
The build pipeline for the [Morgan And Grand Iron Clojure](https://github.com/nasser/magic) compiler.

This repository aims to simplify ongoing work on MAGIC by providing a single place to reproducibly build the compiler, its dependencies, and dependents.

## Subprojects
MAGIC is a complex project consisting of multiple subprojects hosted in their own repositories with their own histories

### Clojure.Runtime
The data structures, reader, and utility functions Clojure uses at runtime. This does not include the Clojure standard library itself, just the functionality that the standard library uses, such as the implementation of Keywords, Vars, all the persistent data structures, and so on.

It is a C# codebase hosted at [nasser/Clojure.Runtime](https://github.com/nasser/Clojure.Runtime) and can be built with the `dotnet` command line tool.

It is forked from [arcadia-unity/clojure-clr](https://github.com/arcadia-unity/clojure-clr) which was forked from the original [clojure/clojure-clr](https://github.com/clojure/clojure-clr). As a result Clojure.Runtime is largely the result of [David Miller](https://github.com/dmiller)'s hard work. MAGIC's Clojure.Runtime is a subset of ClojureCLR, removing the C# implementation of the ClojureCLR compiler and making other changes to better work with MAGIC, particularly around how namespaces and files are loaded.

### Magic.Runtime
Additional data structures and utility functionality used by MAGIC. Its primary contribution is to provide the implementation of fast dynamic call sites.

It is a C# codebase hosted at [nasser/Magic.Runtime](https://github.com/nasser/Magic.Runtime) and can be built with the `dotnet` command line tool.

The decision was made not to add new functionality to Clojure.Runtime to make ingesting upstream updates easier, so anything "new" that MAGIC needs tends to end up in this runtime instead.

### MAGIC
The MAGIC compiler itself, a pure Clojure transformation of s-expressions to MSIL bytecode. 

This repository also includes a patched Clojure standard library. It is modified for performance and to work with MAGIC's different semantics around type hints. It also provides more flexible mechanisms around file and namespace loading to accommodate MAGIC's self-hosting and running on restrictive platforms like iOS. The modifications are designed to work with Clojure.Runtime.

MAGIC is a pure Clojure codebase hosted at [nasser/magic](https://github.com/nasser/magic) and must be built with Nostrand. It depends on [nasser/mage](https://github.com/nasser/mage) and [clojure/tools.analyzer](https://github.com/clojure/tools.analyzer) which are pulled in by Nostrand during the build.

### Nostrand
A task runner, dependency manager and REPL for Clojure on Mono. Something like Leiningen for MAGIC and the C# runtime.

It is a hybrid C# and Clojure codebase hosted at [nasser/nostrand](https://github.com/nasser/nostrand). Its Clojure parts run through MAGIC, so any change to MAGIC or either of its runtimes requires a rebuild of Nostrand.

### Magic.Unity
An integration package to get MAGIC compiled Clojure code running in Unity games, including iOS builds.

It is a C# codebase hosted at [nasser/Magic.Unity](https://github.com/nasser/Magic.Unity) and can be built with the `dotnet` command line tool. It includes bytecode post-processing to work around Unity specific issues with MAGIC compiled code.

## Requirements

* [`git`](https://git-scm.com/)
* [`dotnet`](https://dotnet.microsoft.com/en-us/download) (version 7 or later)
* [`mono`](https://www.mono-project.com/)

## Usage

```
git clone https://github.com/magic-clojure/magic.git
cd magic
dotnet build
```

This will clone MAGIC and its subprojects locally, then build and bootstrap them in the correct order. The result should be the following folders corresponding to the subprojects:

* `clojure.runtime`
* `magic.runtime`
* `magic`
* `nostrand`
* `magic.unity`

A usable Nostrand executable should be available at `nostrand/bin/x64/Release/net471/Nostrand.exe` which can be used with mono to run tasks and compile Clojure code to optimized DLL files. These files can be used in a Unity project that uses the integration found in the `magic.unity` folder. The other folders are of interest to compiler developers only.

# Legal
Copyright © 2015-2022 Ramsey Nasser and contributers

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.