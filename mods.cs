using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Collections.Generic;

// You can add more classes, if you add more files, you'll also have to modify "modify.sh" in tools
class Program
{   
    static AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly("to_mod.dll");
    // you can declare/define functions for modification here

    static void Main()
    {
        // Invoke modifications here
        assembly.Write("modified_assembly.dll");
    }
}

