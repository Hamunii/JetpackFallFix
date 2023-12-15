## Building

To build the project, you need to place the following dll files into the dlls directory:
- Assembly-CSharp.dll
    - However, make sure to publicize it first https://github.com/BepInEx/BepInEx.AssemblyPublicizer
- Unity.Netcode.Runtime.dll

These can be found in the `Lethal Company/Lethal Company_Data/Managed/` directory.

The project should now build successfully, and your shiny new dll file can be found in `bin/Release/netstandard2.1/`