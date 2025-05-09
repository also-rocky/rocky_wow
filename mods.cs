using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly("to_mod.dll");
    public static void insertLdstrAtStart(string className, string methodName, string stringValue)
    {
        // Get the type (class) that contains the method
        var type = assembly.MainModule.GetType(className);

        if (type != null)
        {
            // Find the method in the class by name
            var method = type.Methods.FirstOrDefault(m => m.Name == methodName);

            if (method != null)
            {
                // Get the method body
                var body = method.Body;

                // Create a new Instruction to load the string onto the stack
                var ldstrInstruction = Instruction.Create(OpCodes.Ldstr, stringValue);

                // Insert the instruction at the beginning of the method's IL
                body.Instructions.Insert(0, ldstrInstruction);

                // Optionally, you can add more instructions after the ldstr if needed
                // For example, if you want to print the string or do something with it:
                // body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, someMethodRef));

                Console.WriteLine($"Added 'ldstr' to the start of method '{methodName}' in class '{className}'.");
            }
            else
            {
                Console.WriteLine($"Method '{methodName}' not found in class '{className}'.");
            }
        }
        else
        {
            Console.WriteLine($"Class '{className}' not found in the assembly.");
        }
    }
    public static void makeMethodPublic(string className, string methodName)
    {
        // Find the type (class) that contains the method
        var type = assembly.MainModule.GetType(className);

        if (type != null)
        {
            // Find the method by name
            var method = type.Methods.FirstOrDefault(m => m.Name == methodName);

            if (method != null)
            {
                // Make the method public
                method.IsPublic = true;

                Console.WriteLine($"Method '{methodName}' in class '{className}' is now public.");
            }
            else
            {
                Console.WriteLine($"Method '{methodName}' not found in class '{className}'.");
            }
        }
        else
        {
            Console.WriteLine($"Class '{className}' not found in the assembly.");
        }
    }
    public static void setReturnValue(MethodDefinition method, object returnValue)
    {
        // Ensure the method's return type matches the provided object type
        if (method.ReturnType.FullName != returnValue.GetType().FullName)
        {
            throw new InvalidOperationException($"The method's return type does not match the provided return type.");
        }

        // Create a new ILProcessor to modify the method's IL
        var ilProcessor = method.Body.GetILProcessor();
        
        // Clear the existing IL instructions
        method.Body.Instructions.Clear();

        // Handle the return type
        if (returnValue is string stringValue)
        {
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, stringValue));  // Load the string onto the stack
        }
        else if (returnValue is int intValue)
        {
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, intValue));  // Load integer onto the stack
        }
        else if (returnValue is bool boolValue)
        {
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, boolValue ? 1 : 0));  // Load boolean (1 for true, 0 for false)
        }
        else if (returnValue is float floatValue)
        {
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_R4, floatValue));  // Load float
        }
        else if (returnValue is double doubleValue)
        {
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_R8, doubleValue));  // Load double
        }
        else
        {
            // For custom types or unsupported types, handle them here
            var typeReference = method.Module.ImportReference(returnValue.GetType());  // Import the type reference for the returnValue
            var newObject = method.Module.ImportReference(returnValue.GetType().GetConstructor(Type.EmptyTypes));  // Assuming a parameterless constructor

            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, newObject));  // Create a new object of the custom type
        }

        // Add the return instruction
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));  // Return the value

        Console.WriteLine($"Method {method.Name} has been modified to return {returnValue}.");
    }
    static void injectCallAtStart(string targetClassName, string targetMethodName, string privateClassName, string privateMethodName)
    {
        // Find the target class and method
        var targetClass = assembly.MainModule.GetType(targetClassName);
        var targetMethod = targetClass.Methods.First(m => m.Name == targetMethodName);

        // Find the private class and private method
        var privateClass = assembly.MainModule.GetType(privateClassName);
        var privateMethod = privateClass.Methods.First(m => m.Name == privateMethodName);

        // Check if the private method is an instance method (non-static) or static
        bool isStatic = privateMethod.IsStatic;

        // If the private method is an instance method, we need to create an instance of the private class
        if (!isStatic)
        {
            // Get the constructor (default or user-defined)
            var constructor = privateClass.Methods.FirstOrDefault(m => m.IsConstructor);
            if (constructor == null)
            {
                // If no constructor is explicitly defined, assume the default constructor
                constructor = privateClass.Methods.First(m => m.IsConstructor && !m.HasParameters);
            }

            // Add code to create an instance of the private class
            var ilProcessor = targetMethod.Body.GetILProcessor();

            // Create an instance of the private class
            var local = new VariableDefinition(privateClass);
            targetMethod.Body.Variables.Add(local);

            // Insert the code to call the constructor and create the instance
            ilProcessor.InsertBefore(targetMethod.Body.Instructions.First(), 
                Instruction.Create(OpCodes.Newobj, constructor));

            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(), 
                Instruction.Create(OpCodes.Stloc, local));
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(), 
            Instruction.Create(OpCodes.Stloc, local));
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(), 
            Instruction.Create(OpCodes.Stloc, local));
                        ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(), 
            Instruction.Create(OpCodes.Stloc, local));

            // Load the instance onto the stack
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(),
            Instruction.Create(OpCodes.Ldloc, local));  
            // Load the instance onto the stack
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(),
            Instruction.Create(OpCodes.Ldloc, local));
            // Load the instance onto the stack
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(),
            Instruction.Create(OpCodes.Ldloc, local));
            // Load the instance onto the stack
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(),
            Instruction.Create(OpCodes.Ldloc, local));

            // Call the non-static private method on the instance
            ilProcessor.InsertAfter(targetMethod.Body.Instructions.First(),
                Instruction.Create(OpCodes.Callvirt, privateMethod)); // Call the private instance method
        }
        else
        {
            // If the method is static, no instance is needed, just call it directly
            var ilProcessor = targetMethod.Body.GetILProcessor();
            ilProcessor.InsertBefore(targetMethod.Body.Instructions.First(),
                Instruction.Create(OpCodes.Call, privateMethod)); // Call the private static method
        }

        // Save the modified assembly (You can now save it outside the scope of this method)
        // e.g., assembly.Write("ModifiedAssembly.dll");
    }
    static TypeReference fieldType(Instruction instruction)
    {
        // Check if the instruction is a stfld (store field) instruction
        if (instruction.OpCode == OpCodes.Stfld)
        {
            // The operand of an stfld instruction is a FieldReference
            var fieldRef = (FieldReference)instruction.Operand;

            // The field's type is in the FieldType property of the FieldReference
            TypeReference fieldType = fieldRef.FieldType;

            return fieldType; // Return the TypeReference of the field
        }
        // If it's not a stfld instruction, return null or handle accordingly
        return null;
    }
    static TypeReference localType(MethodBody method, int index)
    {
        // Ensure the index is valid
        if (index < 0 || index >= method.Variables.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Invalid local variable index.");
        }

        // Get the local variable at the specified index
        var localVariable = method.Variables[index];

        // Return the TypeReference of the local variable
        return localVariable.VariableType;
    }
    static void addLocal(MethodBody methodBody, TypeReference localType)
    {
        // Create a new VariableDefinition (which is used for local variables)
        VariableDefinition localVar = new VariableDefinition(localType);

        // Add the local variable to the method's variables collection
        methodBody.Variables.Add(localVar);
    }

    static void copyMethodBody(MethodDefinition sourceMethod, MethodDefinition targetMethod)
    {
        // Clear the existing body of the target method
        targetMethod.Body = new MethodBody(targetMethod);

        // Copy local variables from the source method to the target method
        foreach (var variable in sourceMethod.Body.Variables)
        {
            targetMethod.Body.Variables.Add(new VariableDefinition(variable.VariableType));
        }

        // Copy instructions from the source method to the target method
        foreach (var instruction in sourceMethod.Body.Instructions)
        {
            targetMethod.Body.Instructions.Add(instruction);
        }

        // Copy exception handlers (try/catch/finally) from the source method to the target method
        foreach (var handler in sourceMethod.Body.ExceptionHandlers)
        {
            targetMethod.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType)
            {
                TryStart = handler.TryStart,
                TryEnd = handler.TryEnd,
                HandlerStart = handler.HandlerStart,
                HandlerEnd = handler.HandlerEnd,
                CatchType = handler.CatchType,
                FilterStart = handler.FilterStart
            });
        }

        // If the method has a return type, ensure that the target method has the same return type
        targetMethod.ReturnType = sourceMethod.ReturnType;
    }
    static void printMethods(string className, string methodName)
    {
        try
        {
            // Find the class containing the method
            var targetType = assembly.MainModule.GetType(className);

            // Check if the class exists
            if (targetType == null)
            {
                Console.WriteLine($"Class '{className}' not found in the assembly.");
                return;
            }

            // Find all methods in this class with the specified name
            var methods = targetType.Methods.Where(m => m.Name == methodName).ToList();

            // Check if any methods were found with the specified name
            if (methods.Count > 0)
            {
                Console.WriteLine($"Found {methods.Count} method(s) named '{methodName}' in class '{className}':");
                foreach (var method in methods)
                {
                    // Print method signature
                    Console.WriteLine($"Method: {method.FullName}");

                    // Print parameter types
                    if (method.Parameters.Count > 0)
                    {
                        Console.WriteLine("Parameters:");
                        foreach (var param in method.Parameters)
                        {
                            Console.WriteLine($"  Parameter: {param.Name}, Type: {param.ParameterType.FullName}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No parameters.");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine($"No method named '{methodName}' found in class '{className}'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    static void removeTutorials() {
        var tutorialSave = assembly.MainModule.GetType("TutorialSaveGame");
        MethodDefinition disableTutorial = tutorialSave.Methods.First(m => m.Name == "get_DisabledAllTutorials");
        setReturnValue(disableTutorial, true);
    }

    public static void increaseMaxStack(MethodBody meth, int instructionIndex, int numInstructionsToInsert)
    {   
        var instructions = meth.Instructions;
        // Validate parameters
        if (instructions == null || instructionIndex < 0 || numInstructionsToInsert <= 0 || instructionIndex >= instructions.Count)
        {
            Console.WriteLine("Invalid parameters.");
            return;
        }

        // Insert `numInstructionsToInsert` instances of `ldloc.0` after the instruction at `instructionIndex`
        for (int k = 0; k < numInstructionsToInsert; k++)
        {
            // Create ldloc.0 instruction
            var ldlocInstruction = Instruction.Create(OpCodes.Ldc_I4, 0);

            // Insert it after the instruction at `instructionIndex + k`
            instructions.Insert(instructionIndex + k, ldlocInstruction);
        }

        // Insert `numInstructionsToInsert` instances of `stloc.0` after the inserted `ldloc.0` instructions
        for (int k = 0; k < numInstructionsToInsert; k++)
        {
            // Create stloc.0 instruction
            var stlocInstruction = Instruction.Create(OpCodes.Pop);

            // Insert it after the `ldloc.0` instructions
            instructions.Insert(instructionIndex + k + numInstructionsToInsert, stlocInstruction);
        }

        // If you need to update the MethodBody's instructions, you can set the new instructions here.
        // Assuming `methodBody` is the original MethodBody object
        // methodBody.Instructions.Clear();
        // foreach (var instruction in instructions)
        // {
        //     methodBody.Instructions.Add(instruction);
        // }
    }

    // not associative nor commutative
    public static void removeInstructions(MethodDefinition meth, int fromIndex, int toIndex) {
        var instructions = meth.Body.Instructions;
        if(toIndex >= instructions.Count) {
            Console.WriteLine("too long");
            return;
        }
        for (int i = toIndex; i >= toIndex ; i--) {
            instructions.RemoveAt(i);
        }
    }

    static void setIdentity() {
        string _GameUserId = "cab35b2c-482f-4024-9b3c-176636154fc5";
        string _SessionToken = "7eb9f821-4571-4475-bdf7-79e228530447";
        string _AnonUserId = "f8a955ee-3e69-4491-b17e-780762f8244e";
        string _FacebookId = "223405451";
        string _FacebookAuthToken = "CAACEdEose0cBANNbBBZC5ECzMTDdhv6VsIXvJMV1OcZBAvH41GCZCNsby2Ahi0BXZCKZBxSDNnqblXxc3vXGzKOZAX9furmY2mv23n4HiHqwgj8YSRj49mVQTjxcyZCrfIiXenGm6lZBbzOryk87FcXTByJsCYEZBH2NaZA2GTUZAHZBZCYxZAIEZBhzn0ZB2zjg87fDLqMZD";
        string ANON_USER_ID_PREFS_KEY = "anonymousUserId";
        string GAME_USER_ID_PREFS_KEY = "gameUserId";
        string SESSION_TOKEN_PREFS_KEY = "sessionToken";

        var bypassId = assembly.MainModule.GetType("IdentityBypassSettings");
        var enableBypass = bypassId.Methods.First(m => m.Name == "get_EnableBypass");
        setReturnValue(enableBypass, true);

        injectCallAtStart("AnonymousIdentityProvider", "get_AnonymousUserID", "PlayerPrefs", "SetString");
        insertLdstrAtStart("AnonymousIdentityProvider", "get_AnonymousUserID", _AnonUserId);
        insertLdstrAtStart("AnonymousIdentityProvider", "get_AnonymousUserID", ANON_USER_ID_PREFS_KEY);

        var anonId = assembly.MainModule.GetType("AnonymousIdentityProvider");
        var isLoggedIn = anonId.Methods.First(m => m.Name == "get_IsLoggedIn");
        setReturnValue(isLoggedIn, true);


        injectCallAtStart("Identity", "get_GameUserId", "PlayerPrefs", "SetString");
        insertLdstrAtStart("Identity", "get_GameUserId", _AnonUserId);
        insertLdstrAtStart("Identity", "get_GameUserId", GAME_USER_ID_PREFS_KEY);

        injectCallAtStart("Identity", "get_SessionToken", "PlayerPrefs", "SetString");
        insertLdstrAtStart("Identity", "get_SessionToken", _SessionToken);
        insertLdstrAtStart("Identity", "get_SessionToken", SESSION_TOKEN_PREFS_KEY);
    }

    static void setPlayerStats() {
        var playerStats = assembly.MainModule.GetType("PlayerStats");
        var data = playerStats.NestedTypes.FirstOrDefault(n => n.Name == "PlayerStatsData");
        var registered = playerStats.Methods.First(m => m.Name == "get_Registered");
        setReturnValue(registered, true);
        Console.WriteLine("playerstats registered");

        var starterPack = playerStats.Methods.First(m => m.Name == "get_SelectedStarterPack").Body.Instructions;

        var getGems = playerStats.Methods.First(m => m.Name == "get_Gems").Body;
        var dataCon = data.Methods.First(m => m.Name == ".ctor").Body;
        var getGemsIntructs = getGems.Instructions;
        getGemsIntructs.Clear();
        //getGemsIntructs.Add(starterPack[9]);
        //getGemsIntructs.Add(starterPack[10]);
        //getGemsIntructs.Add(starterPack[11]);
        getGemsIntructs.Add(Instruction.Create(OpCodes.Ldc_I4, 1000));
        getGemsIntructs.Add(dataCon.Instructions[17]);
        getGemsIntructs.Add(Instruction.Create(OpCodes.Ret));
        Console.WriteLine("playerstats fixset gems");

        var getStamina = playerStats.Methods.First(m => m.Name == "get_Stamina").Body;
        var getStamInstr = getStamina.Instructions;
        getStamInstr.Clear();
        getStamInstr.Add(Instruction.Create(OpCodes.Ldc_I4, 1000));
        getStamInstr.Add(dataCon.Instructions[17]);
        getStamInstr.Add(Instruction.Create(OpCodes.Ret));


        getStamina = playerStats.Methods.First(m => m.Name == "get_Coins").Body;
        getStamInstr = getStamina.Instructions;
        getStamInstr.Clear();
        getStamInstr.Add(Instruction.Create(OpCodes.Ldc_I4, 100000));
        getStamInstr.Add(dataCon.Instructions[17]);
        getStamInstr.Add(Instruction.Create(OpCodes.Ret));


        var getUsername = playerStats.Methods.First(m => m.Name == "get_Username");
        setReturnValue(getUsername, "Garp");
        Console.WriteLine("playerstats username set");
    }

    public static void gachaScreen() {

        var switcher = assembly.MainModule.GetType("SwitchScreenIfOnline");
        var gotConfig = switcher.Methods.First(m=>m.Name == "GotGachaConfig");
        gotConfig.Attributes |= MethodAttributes.Public;
        var switcherCtor = switcher.Methods.First(m=>m.Name == ".ctor");
        var loggedIn = switcher.Methods.First(m=>m.Name == "LoggedIn");
        var instr = loggedIn.Body.Instructions;
        // call gotConfig
        instr.Insert(20, Instruction.Create(OpCodes.Ldarg_0));
        instr.Insert(21, Instruction.Create(OpCodes.Call, gotConfig));
        instr.Insert(22, Instruction.Create(OpCodes.Ret));
        increaseMaxStack(loggedIn.Body, 25, 5);

        instr = gotConfig.Body.Instructions;
        var trigger = switcher.Methods.First(m=>m.Name == "Trigger").Body.Instructions;
        for(int i = instr.Count-1; i > -1; i--) {
            trigger.Insert(41, instr[i]);
        }
        //remove for testing in tutorial
        for(int i = 0; i < 31; i++) {
            trigger.RemoveAt(0);
        }
        increaseMaxStack(switcher.Methods.First(m=>m.Name == "Trigger").Body, trigger.Count-1, 10);



        /*var gachaServ = assembly.MainModule.GetType("GachaService");
        var gachaCost = gachaServ.NestedTypes.First(m=>m.Name == "GachaCost");
        //tmp 
        var getCurr = gachaCost.Methods.First(m=>m.Name == "get_Currency").Body.Instructions;
        getCurr.Clear();
        getCurr.Add(Instruction.Create(OpCodes.Ldstr, "RedGems"));
        getCurr.Add(Instruction.Create(OpCodes.Ret));
        var getAmount = gachaCost.Methods.First(m=>m.Name == "get_Amount").Body.Instructions;
        getAmount.Clear();
        getAmount.Add(Instruction.Create(OpCodes.Ldc_I4, 5));
        getAmount.Add(Instruction.Create(OpCodes.Ret));*/
        
        /*
        var gachaCostCtor = gachaCost.Methods.First(m=>m.Name == ".ctor");
        var gacha = assembly.MainModule.GetType("GachaController");
        var fetch = gacha.Methods.First(m=>m.Name == "FetchConfig").Body.Instructions;
        var pCost = gacha.Fields.First(m=>m.Name == "_premiumCost");
        var sCost = gacha.Fields.First(m=>m.Name == "_socialCost");
        var western = gacha.Fields.First(m=>m.Name == "_cachedWesternDoorConfig");
        fetch.Clear();
        // set premium cost
        fetch.Add(Instruction.Create(OpCodes.Ldarg_0));
        fetch.Add(Instruction.Create(OpCodes.Newobj, gachaCostCtor));
        fetch.Add(Instruction.Create(OpCodes.Stfld, pCost));
        //set social cost
        fetch.Add(Instruction.Create(OpCodes.Ldarg_0));
        fetch.Add(Instruction.Create(OpCodes.Newobj, gachaCostCtor));
        fetch.Add(Instruction.Create(OpCodes.Stfld, sCost));
        // western config to null
        fetch.Add(Instruction.Create(OpCodes.Ldarg_0));
        fetch.Add(Instruction.Create(OpCodes.Ldnull));
        fetch.Add(Instruction.Create(OpCodes.Stfld, western));
        //call GotGachaConfig
        fetch.Add(Instruction.Create(OpCodes.Newobj, switcherCtor));
        fetch.Add(Instruction.Create(OpCodes.Call, gotConfig));
        // done
        fetch.Add(Instruction.Create(OpCodes.Ret));
        increaseMaxStack(gacha.Methods.First(m=>m.Name == "FetchConfig").Body, fetch.Count, 5);*/

    }

    public static void appendForStack(MethodDefinition meth, int amount) {
        for(int i = 0; i < amount; i++) {
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        }
        for(int i = 0; i < amount; i++) {
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
    }

    // somehow small mistake snuck in
    public static void triggerGacha() {
        var playerStats = assembly.MainModule.GetType("PlayerStats");
        var random = playerStats.Methods.First(m => m.Name == "get_SelectedStarterPack").Body.Instructions;
        
        var starterPack = playerStats.Methods.First(m => m.Name == "get_StarterPackActive").Body.Instructions;
        starterPack.Clear();
        starterPack.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
        starterPack.Add(Instruction.Create(OpCodes.Ret));
        

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheatInstr = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter").Body.Instructions;
        
        //var gachaData = assembly.MainModule.GetType("GachaData");
        //var random = gachaData.NestedTypes.First(m => m.Name == "eRarity");

        var gacha = assembly.MainModule.GetType("GachaController");
        var test = gacha.Methods.First(m=>m.Name == "SetStarterPackWarrior");
        var awardPrize = gacha.Methods.First(m=>m.Name == "AwardPrize");
        var trigger = gacha.Methods.First(m=>m.Name == "TriggerGacha");
        addLocal(trigger.Body, localType(test.Body, 0));
        var setGacha = test.Body.Instructions;
        var instrs = trigger.Body.Instructions;
        //remove unecassery end part
        for(int i = instrs.Count-1 ; i > 2; i--) {
            instrs.RemoveAt(i);
        }

        // set gacha prize type
        instrs.Add(setGacha[0]);
        instrs.Add(setGacha[1]);
        instrs.Add(setGacha[2]);

        
        // load warriorcollection
        instrs.Add(cheatInstr[12]);
        instrs.Add(cheatInstr[13]);

        // load amount of warriors
        instrs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instrs.Add(cheatInstr[23]);
        instrs.Add(cheatInstr[24]);
        instrs.Add(cheatInstr[25]);


        // random value in that range
        instrs.Add(random[12]);

        // get random warrior
        instrs.Add(cheatInstr[15]);

        for(int i = 14; i < setGacha.Count-1; i++) {
            instrs.Add(setGacha[i]);
        }
        
        instrs.Add(Instruction.Create(OpCodes.Ldarg_0));
        instrs.Add(Instruction.Create(OpCodes.Ldnull));
        instrs.Add(Instruction.Create(OpCodes.Call, awardPrize));

        instrs.Add(Instruction.Create(OpCodes.Ret));
        appendForStack(trigger, 10);

    }

    public static void gachaByIndex() {
        var playerStats = assembly.MainModule.GetType("PlayerStats");
        var random = playerStats.Methods.First(m => m.Name == "get_SelectedStarterPack").Body.Instructions;

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheatInstr = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter").Body.Instructions;
        
        //var gachaData = assembly.MainModule.GetType("GachaData");
        //var random = gachaData.NestedTypes.First(m => m.Name == "eRarity");

         var field = new FieldDefinition(
            "_index",                    // Field name
            FieldAttributes.Public | FieldAttributes.Static, // Field attributes (Public and Static)
            assembly.MainModule.ImportReference(typeof(int)) // Field type (in this case, int)
        );

        var gacha = assembly.MainModule.GetType("GachaController");
        gacha.Fields.Add(field);
        var index = gacha.Fields.First(m=>m.Name == "_index");
        var cctor = gacha.Methods.First(m=>m.Name == ".cctor").Body.Instructions;
        cctor.Clear();
        cctor.Add(Instruction.Create(OpCodes.Ldc_I4, 3));
        cctor.Add(Instruction.Create(OpCodes.Stsfld, index));
        cctor.Add(Instruction.Create(OpCodes.Ret));


        var test = gacha.Methods.First(m=>m.Name == "SetStarterPackWarrior");
        var trigger = gacha.Methods.First(m=>m.Name == "TriggerGacha");
        addLocal(trigger.Body, localType(test.Body, 0));
        var setGacha = test.Body.Instructions;
        var instrs = trigger.Body.Instructions;
        //remove unecassery end part
        for(int i = instrs.Count-1 ; i > 11; i--) {
            instrs.RemoveAt(i);
        }

        // load warriorcollection
        instrs.Add(cheatInstr[12]);
        instrs.Add(cheatInstr[13]);

        instrs.Add(Instruction.Create(OpCodes.Ldsfld, index));

        // get index warrior
        instrs.Add(cheatInstr[15]);

        for(int i = 14; i < setGacha.Count-1; i++) {
            instrs.Add(setGacha[i]);
        }

        instrs.Add(instrs[8]);
        instrs.Add(instrs[9]);
        instrs.Add(instrs[10]);

        // increment index
       instrs.Add(Instruction.Create(OpCodes.Ldsfld, index));
       instrs.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
       instrs.Add(Instruction.Create(OpCodes.Add));
       instrs.Add(Instruction.Create(OpCodes.Stsfld, index));


        instrs.Add(instrs[11]);

        appendForStack(trigger, 10);


    }

    static void getWarriors() {
        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheat = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter");
        var cheatInstr = cheat.Body.Instructions;
        
        var warriors = assembly.MainModule.GetType("WarriorCollectionSaveGame");
        var data = warriors.NestedTypes.First(m=>m.Name == "Data");
        var dictionary = data.Fields.First(m=>m.Name == "_CollectionData");
        var collect = warriors.Methods.First(m=>m.Name == "CollectibleCollected");
        var collectInstr = collect.Body.Instructions;
        var ctordata = data.Methods.First(m=>m.Name == ".ctor");

        ctordata.Body.InitLocals = true;
        addLocal(ctordata.Body, localType(cheat.Body,0));
        addLocal(ctordata.Body, localType(cheat.Body,1));
        addLocal(ctordata.Body, localType(cheat.Body,2));
        addLocal(ctordata.Body, dictionary.FieldType);

        var instructs = ctordata.Body.Instructions;
        instructs.RemoveAt(instructs.Count - 1);

        // store dictionary locally
        instructs.RemoveAt(instructs.Count - 1);
        instructs.Add(Instruction.Create(OpCodes.Stloc_3));
        
        // storeloc instead of stfld
        //instructs.RemoveAt(instructs.Count - 2);
        //instructs.Add(Instruction.Create(OpCodes.Stloc_3));

        // initialise counter
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(Instruction.Create(OpCodes.Stloc_0));

        //jumpt to comparison
        //instructs.Add(cheatInstr[10]);

        // get collectibleStats at counter
        instructs.Add(cheatInstr[12]);
        instructs.Add(cheatInstr[13]);
        instructs.Add(cheatInstr[14]);
        instructs.Add(cheatInstr[15]);
        // store collectibeStats
        instructs.Add(Instruction.Create(OpCodes.Stloc_2));


        // get dictionary for ::setItem() (arg0)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        // get warrior Id for ::setItem() (arg1)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[22]);
        // get collectibleWarrior for ::setItem() (arg2)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[7]);
        //call ::setItem()
        instructs.Add(collectInstr[24]);


        //increment counter
        instructs.Add(cheatInstr[18]);
        instructs.Add(cheatInstr[19]);
        instructs.Add(cheatInstr[20]);
        instructs.Add(cheatInstr[21]);
        instructs.Add(cheatInstr[22]);

        // compare counter with size of list
        instructs.Add(cheatInstr[23]);
        instructs.Add(cheatInstr[24]);
        instructs.Add(cheatInstr[25]);
        // if counter < list.size, go again
        instructs.Add(cheatInstr[26]);
        var bltTarget = instructs[instructs.Count-1].Operand as Instruction;
        bltTarget = instructs[instructs.Count-20];
        instructs[instructs.Count-1].Operand = bltTarget = bltTarget;
        
        //instructs.Add(Instruction.Create(OpCodes.Pop));

        //store dictionary
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        instructs.Add(instructs[2]);
        instructs.Add(Instruction.Create(OpCodes.Ret));
    
        increaseMaxStack(ctordata.Body, 0, 22);




    }

    public static void enableTouch() {
        var toggle = assembly.MainModule.GetType("TutorialToggleInputFrontend");
        var trigger = toggle.Methods.First(m => m.Name == "Trigger").Body.Instructions;
        for(int i = 0; i < 5; i++) {
            trigger.RemoveAt(0);
        }
        trigger.Insert(0, Instruction.Create(OpCodes.Ldc_I4, 0));

        toggle = assembly.MainModule.GetType("TutorialToggleInput");
        trigger = toggle.Methods.First(m => m.Name == "Trigger").Body.Instructions;
        for(int i = 0; i < 7; i++) {
            trigger.RemoveAt(0);
        }
    }

    static void getWarriorsFiltered() {
        var controller = assembly.MainModule.GetType("GachaController");
        var getStats = controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body.Instructions;

        var btru = assembly.MainModule.GetType("ElementalShiftSpecialSettings");
        var brtrue = btru.Methods.First(m=>m.Name == "PushSettingsToMechanic").Body.Instructions;

        var Wcheats = assembly.MainModule.GetType("WarriorConsoleCheats");
        var strings = Wcheats.Methods.First(m=>m.Name == "AwardWarrior").Body.Instructions;

        var playStats = assembly.MainModule.GetType("PlayerStats");
        var startPack = playStats.Methods.First(m=>m.Name == "get_SelectedStarterPack").Body;
        startPack.Instructions[12] = Instruction.Create(OpCodes.Ldc_I4, 0);
        startPack.Instructions.RemoveAt(11);
        startPack.Instructions.RemoveAt(10);
        startPack.Instructions.RemoveAt(9);
        startPack.Instructions.RemoveAt(8);
        increaseMaxStack(startPack, 6, 10);

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheat = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter");
        var cheatInstr = cheat.Body.Instructions;
        
        var warriors = assembly.MainModule.GetType("WarriorCollectionSaveGame");
        var data = warriors.NestedTypes.First(m=>m.Name == "Data");
        var dictionary = data.Fields.First(m=>m.Name == "_CollectionData");
        var collect = warriors.Methods.First(m=>m.Name == "CollectibleCollected");
        var collectInstr = collect.Body.Instructions;
        var ctordata = data.Methods.First(m=>m.Name == ".ctor");

        ctordata.Body.InitLocals = true;
        addLocal(ctordata.Body, localType(cheat.Body,0));
        addLocal(ctordata.Body, localType(cheat.Body,1));
        addLocal(ctordata.Body, localType(cheat.Body,2));
        addLocal(ctordata.Body, dictionary.FieldType);

        var instructs = ctordata.Body.Instructions;
        instructs.RemoveAt(instructs.Count - 1);

        // store dictionary locally
        instructs.RemoveAt(instructs.Count - 1);
        instructs.Add(Instruction.Create(OpCodes.Stloc_3));

    
        // initialise counter (index 52)
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(Instruction.Create(OpCodes.Stloc_0));

        //jumpt to comparison
        //instructs.Add(cheatInstr[10]);

        // get collectibleStats at counter 
        instructs.Add(cheatInstr[12]); //10
        instructs.Add(cheatInstr[13]);
        instructs.Add(cheatInstr[14]);
        instructs.Add(cheatInstr[15]);
        // store collectibeStats
        instructs.Add(Instruction.Create(OpCodes.Stloc_2)); //15

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //16
        instructs.Add(collectInstr[22]); 
        // get abu id
        instructs.Add(getStats[3]); //18
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,0));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 25
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]); //26
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //27
        instructs.Add(collectInstr[22]);
        // get sakuma id
        instructs.Add(getStats[3]); //29 
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,1));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); //36
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]);
        instructs.Add(Instruction.Create(OpCodes.Or)); //38
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //39
        instructs.Add(collectInstr[22]);
        // get ram id
        instructs.Add(getStats[3]); //41
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,2));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 48
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]);
        instructs.Add(Instruction.Create(OpCodes.Or)); // //50
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //51
        instructs.Add(collectInstr[22]);
        // get ram id
        instructs.Add(getStats[3]); //53
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,3));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 60
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]); 
        instructs.Add(Instruction.Create(OpCodes.Or)); // 61
        instructs.Add(brtrue[6]); // 62
        // Operand is adjusted later


        // get dictionary for ::setItem() (arg0) 
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        // get warrior Id for ::setItem() (arg1)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[22]);
        // get collectibleWarrior for ::setItem() (arg2)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[7]);
        //call ::setItem()
        instructs.Add(collectInstr[24]);


        //increment counter
        instructs.Add(cheatInstr[18]);
        instructs[62].Operand = instructs[instructs.Count - 1];

        instructs.Add(cheatInstr[19]);
        instructs.Add(cheatInstr[20]);
        instructs.Add(cheatInstr[21]);
        instructs.Add(cheatInstr[22]);

        // compare counter with size of list
        instructs.Add(cheatInstr[23]);
        instructs.Add(cheatInstr[24]);
        instructs.Add(cheatInstr[25]);
        // if counter < list.size, go again
        instructs.Add(cheatInstr[26]);
        var bltTarget = instructs[instructs.Count-1].Operand as Instruction;
        bltTarget = instructs[10];
        instructs[instructs.Count-1].Operand = bltTarget = bltTarget;
        
        //instructs.Add(Instruction.Create(OpCodes.Pop));

        //store dictionary
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        instructs.Add(instructs[2]);
        instructs.Add(Instruction.Create(OpCodes.Ret));
    
        increaseMaxStack(ctordata.Body, 0, 22);




    }

    static void getStarter(int packIndex, int pullIndex) {
        var controller = assembly.MainModule.GetType("GachaController");
        var getStats = controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body.Instructions;

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheat = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter");
        var cheatInstr = cheat.Body.Instructions;
        
        var warriors = assembly.MainModule.GetType("WarriorCollectionSaveGame");
        var data = warriors.NestedTypes.First(m=>m.Name == "Data");
        var dictionary = data.Fields.First(m=>m.Name == "_CollectionData");
        var collect = warriors.Methods.First(m=>m.Name == "CollectibleCollected");
        var collectInstr = collect.Body.Instructions;
        var ctordata = data.Methods.First(m=>m.Name == ".ctor");

        ctordata.Body.InitLocals = true;
        addLocal(ctordata.Body, localType(cheat.Body,0));
        addLocal(ctordata.Body, localType(cheat.Body,1));
        addLocal(ctordata.Body, localType(cheat.Body,2));
        addLocal(ctordata.Body, dictionary.FieldType);

        var instructs = ctordata.Body.Instructions;
        instructs.RemoveAt(instructs.Count - 1);

        // store dictionary locally
        instructs.RemoveAt(instructs.Count - 1);
        instructs.Add(Instruction.Create(OpCodes.Stloc_3));

        // initialise counter
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(Instruction.Create(OpCodes.Stloc_0));

        //jumpt to comparison
        //instructs.Add(cheatInstr[10]);

        // get collectibleStats at counter
        instructs.Add(getStats[3]);
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, packIndex));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, pullIndex));
        instructs.Add(getStats[13]);

        // store collectibeStats
        instructs.Add(Instruction.Create(OpCodes.Stloc_2));


        // get dictionary for ::setItem() (arg0)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        // get warrior Id for ::setItem() (arg1)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[22]);
        // get collectibleWarrior for ::setItem() (arg2)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[7]);
        //call ::setItem()
        instructs.Add(collectInstr[24]);


        //increment counter
        /*instructs.Add(cheatInstr[18]);
        instructs.Add(cheatInstr[19]);
        instructs.Add(cheatInstr[20]);
        instructs.Add(cheatInstr[21]);
        instructs.Add(cheatInstr[22]);

        // compare counter with size of list
        instructs.Add(cheatInstr[23]);
        instructs.Add(cheatInstr[24]);
        instructs.Add(cheatInstr[25]);
        
        // if counter < list.size, go again
        instructs.Add(cheatInstr[26]);
        var bltTarget = instructs[instructs.Count-1].Operand as Instruction;
        bltTarget = instructs[instructs.Count-20];
        instructs[instructs.Count-1].Operand = bltTarget = bltTarget;*/
        
        //instructs.Add(Instruction.Create(OpCodes.Pop));

        //store dictionary
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        instructs.Add(instructs[2]);
        instructs.Add(Instruction.Create(OpCodes.Ret));
    
        increaseMaxStack(ctordata.Body, 0, 22);




    }

    static void getStarting() {
        int[] indices = {103, 43, 29, 10};
        var controller = assembly.MainModule.GetType("GachaController");
        var getStats = controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body.Instructions;

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheat = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter");
        var cheatInstr = cheat.Body.Instructions;
        addLocal(controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body, localType(cheat.Body, 0));
        addLocal(controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body, localType(cheat.Body, 0));

        // get pull index on stack and store on stack
        getStats.RemoveAt(13);

        for(int i = 9; i > 2; i--) {
            getStats.RemoveAt(i);
        }
        getStats.Insert(6, Instruction.Create(OpCodes.Stloc_1));
        // index 5

        
        // case pullIndex = 0
        getStats.Insert(7, Instruction.Create(OpCodes.Ldloc_1));
        getStats.Insert(8, Instruction.Create(OpCodes.Ldc_I4, 0));
        getStats.Insert(9, Instruction.Create(OpCodes.Ceq));
        // if unequal check next case
        getStats.Insert(10, Instruction.Create(OpCodes.Brfalse, getStats[0])); // adjust later
        // else go store warrior index and jump to get it
        getStats.Insert(11, Instruction.Create(OpCodes.Ldc_I4, indices[0]));
        getStats.Insert(12, Instruction.Create(OpCodes.Stloc_2));
        getStats.Insert(13, Instruction.Create(OpCodes.Br, getStats[0])); // adjust later

        // case pullIndex = 1
        getStats.Insert(14, Instruction.Create(OpCodes.Ldloc_1));
        getStats.Insert(15, Instruction.Create(OpCodes.Ldc_I4, 1));
        getStats.Insert(16, Instruction.Create(OpCodes.Ceq));
        // if unequal check next case
        getStats.Insert(17, Instruction.Create(OpCodes.Brfalse, getStats[0])); // adjust later
        // else go store warrior index and jump to get it
        getStats.Insert(18, Instruction.Create(OpCodes.Ldc_I4, indices[1]));
        getStats.Insert(19, Instruction.Create(OpCodes.Stloc_2));
        getStats.Insert(20, Instruction.Create(OpCodes.Br, getStats[0])); // adjust later

        // case pullIndex = 2
        getStats.Insert(21, Instruction.Create(OpCodes.Ldloc_1));
        getStats.Insert(22, Instruction.Create(OpCodes.Ldc_I4, 2));
        getStats.Insert(23, Instruction.Create(OpCodes.Ceq));
        // if unequal check next case
        getStats.Insert(24, Instruction.Create(OpCodes.Brfalse, getStats[0])); // adjust later
        // else go store warrior index and jump to get it
        getStats.Insert(25, Instruction.Create(OpCodes.Ldc_I4, indices[2]));
        getStats.Insert(26, Instruction.Create(OpCodes.Stloc_2));
        getStats.Insert(27, Instruction.Create(OpCodes.Br, getStats[0])); // adjust later

        // case pullIndex = 3    
        getStats.Insert(28, Instruction.Create(OpCodes.Ldc_I4, indices[3]));
        getStats.Insert(29, Instruction.Create(OpCodes.Stloc_2));

        
        // get collectiblestats
        getStats.Insert(30, cheatInstr[12]);
        getStats.Insert(31, cheatInstr[13]);
        getStats.Insert(32, Instruction.Create(OpCodes.Ldloc_2));
        getStats.Insert(33, cheatInstr[15]);

        getStats[13].Operand = getStats[30];
        getStats[20].Operand = getStats[30];
        getStats[27].Operand = getStats[30];

        getStats[10].Operand = getStats[14];
        getStats[17].Operand = getStats[21];
        getStats[24].Operand = getStats[28];

        increaseMaxStack(controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body, 0, 10);

    }

    /* dont uncomment
    static void setStarting() {
        var warriors = assembly.MainModule.GetType("WarriorCollectionSaveGame");
        var collect = warriors.Methods.First(m=>m.Name == "CollectibleCollected").Body.Instructions;
        var instance = warriors.Methods.First(m=>m.Name == "get_Instance");
        var dict = warriors.Methods.First(m=>m.Name == "get_CollectionData");


        // get_stats
        var collectible = assembly.MainModule.GetType("CollectibleWarrior");
        var getStats = collectible.Methods.First(m=>m.Name == "get_Stats");

        // field for loading rank
        var warStats = assembly.MainModule.GetType("CollectibleWarriorStats");
        var rank = warStats.Fields.First(m=>m.Name == "_WarriorRank");

        var controller = assembly.MainModule.GetType("GachaController");

        var award = controller.Methods.First(m=>m.Name == "AwardWarrior").Body.Instructions;
        Instruction[] pullIndex = {award[31], award[32], award[33]};


        for(int i = 35; i >= 29; i--) {
            award.RemoveAt(i);
        }

        // non tutorial collect
        award.Insert(34, award[22]);
        award.Insert(35, award[23]);
        award.Insert(36, Instruction.Create(OpCodes.Callvirt, warriors.Methods.First(m=>m.Name == "CollectibleCollected"));
        award.Insert(37, Instruction.Create(OpCodes.Stloc_0);

        
        // get dictionary for ::setItem() (arg0) 
        award.Insert(34, Instruction.Create(OpCodes.Call, instance));
        award.Insert(35, Instruction.Create(OpCodes.Call, dict));
        // get warrior Id for ::setItem() (arg1)
        award.Insert(36, Instruction.Create(OpCodes.Ldarg_0));
        // get collectibleWarrior for ::setItem() (arg2)
        award.Insert(37, Instruction.Create(OpCodes.Ldloc_0));
        //call ::setItem()
        award.Insert(38, collect[24]);
        //jump over normal collect handler
        award.Insert(39, Instruction.Create(OpCodes.Br, award[0])); // set later

        // simply get Collectible instead of calling collectibleWarr
        award[24] = collect[7];

        // case fourth pull
        award.Insert(29, pullIndex[0]);
        award.Insert(30, pullIndex[1]);
        award.Insert(31, pullIndex[2]);
        award.Insert(32, Instruction.Create(OpCodes.Ldc_I4, 3));
        award.Insert(33, Instruction.Create(OpCodes.Ceq));
        award.Insert(34, Instruction.Create(OpCodes.Brfalse, award[0])); // jumpt to other cases handler
        // handle fourth pull case
        award.Insert(35, Instruction.Create(OpCodes.Ldloc_0));
        award.Insert(36, Instruction.Create(OpCodes.Call, getStats));
        award.Insert(37, Instruction.Create(OpCodes.Ldc_I4, 1));
        award.Insert(38, Instruction.Create(OpCodes.Stfld, rank));
        award.Insert(39, Instruction.Create(OpCodes.Br, award[0])); // jump over other cases handler

        //handle other cases
        award.Insert(40, Instruction.Create(OpCodes.Ldloc_0));
        award.Insert(41, Instruction.Create(OpCodes.Call, getStats));
        award.Insert(42, Instruction.Create(OpCodes.Ldc_I4, 0));
        award.Insert(43, Instruction.Create(OpCodes.Stfld, rank)); // 15
        appendForStack(controller.Methods.First(m=>m.Name == "AwardWarrior"), 10);
        award[28].Operand = award[55];
        award[34].Operand = award[40];
        award[39].Operand = award[44];
        
    }*/

    static void hasSauce() {
        var Wrecipe = assembly.MainModule.GetType("WarriorRecipe");
        var recipeCtor = Wrecipe.Methods.First(m=>m.Name == ".ctor");

        var collectible = assembly.MainModule.GetType("CollectibleWarrior");
        var recipe = collectible.Methods.First(m=>m.Name == "get_Recipe");
        var recipeInstr = recipe.Body.Instructions;
        recipeInstr.Clear();
        recipeInstr.Add(Instruction.Create(OpCodes.Newobj, recipeCtor));
        recipeInstr.Add(Instruction.Create(OpCodes.Ret));

        /*var evolve = collectible.Methods.First(m=>m.Name == "get_WarriorReadyToEvolve").Body.Instructions;
        evolve.Clear();
        evolve.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
        evolve.Add(Instruction.Create(OpCodes.Ret));*/
        /*
        var pro = assembly.MainModule.GetType("WarriorTrainingProgressionData");
        var buyXp = pro.Methods.First(m=>m.Name == "get_CanBuyXP").Body.Instructions;
        buyXp.Clear();
        buyXp.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
        buyXp.Add(Instruction.Create(OpCodes.Ret));
        */
        /*recipe = collectible.Methods.First(m=>m.Name == "get_WarriorRankIndex");
        recipeInstr = recipe.Body.Instructions;
        recipeInstr.Clear();
        recipeInstr.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        recipeInstr.Add(Instruction.Create(OpCodes.Ret));*/



    }

    static void getWarriorsMaxed() {
        var controller = assembly.MainModule.GetType("GachaController");
        var getStats = controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body.Instructions;

        var btru = assembly.MainModule.GetType("ElementalShiftSpecialSettings");
        var brtrue = btru.Methods.First(m=>m.Name == "PushSettingsToMechanic").Body.Instructions;

        var Wcheats = assembly.MainModule.GetType("WarriorConsoleCheats");
        var strings = Wcheats.Methods.First(m=>m.Name == "AwardWarrior").Body.Instructions;

        var playStats = assembly.MainModule.GetType("PlayerStats");
        var startPack = playStats.Methods.First(m=>m.Name == "get_SelectedStarterPack").Body;
        startPack.Instructions[12] = Instruction.Create(OpCodes.Ldc_I4, 0);
        startPack.Instructions.RemoveAt(11);
        startPack.Instructions.RemoveAt(10);
        startPack.Instructions.RemoveAt(9);
        startPack.Instructions.RemoveAt(8);
        increaseMaxStack(startPack, 6, 10);

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheat = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter");
        var cheatInstr = cheat.Body.Instructions;
        
        var warriors = assembly.MainModule.GetType("WarriorCollectionSaveGame");
        var data = warriors.NestedTypes.First(m=>m.Name == "Data");
        var dictionary = data.Fields.First(m=>m.Name == "_CollectionData");
        var collect = warriors.Methods.First(m=>m.Name == "CollectibleCollected");
        var collectInstr = collect.Body.Instructions;
        var ctordata = data.Methods.First(m=>m.Name == ".ctor");

        ctordata.Body.InitLocals = true;
        addLocal(ctordata.Body, localType(cheat.Body,0));
        addLocal(ctordata.Body, localType(collect.Body,0));
        addLocal(ctordata.Body, localType(cheat.Body,2));
        addLocal(ctordata.Body, dictionary.FieldType);

        var instructs = ctordata.Body.Instructions;
        instructs.RemoveAt(instructs.Count - 1);

        // store dictionary locally
        instructs.RemoveAt(instructs.Count - 1);
        instructs.Add(Instruction.Create(OpCodes.Stloc_3));

    
        // initialise counter (index 52)
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(Instruction.Create(OpCodes.Stloc_0));

        //jumpt to comparison
        //instructs.Add(cheatInstr[10]);

        // get collectibleStats at counter 
        instructs.Add(cheatInstr[12]); //10
        instructs.Add(cheatInstr[13]);
        instructs.Add(cheatInstr[14]);
        instructs.Add(cheatInstr[15]);
        // store collectibeStats
        instructs.Add(Instruction.Create(OpCodes.Stloc_2)); //15

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //16
        instructs.Add(collectInstr[22]); 
        // get abu id
        instructs.Add(getStats[3]); //18
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,0));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 25
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]); //26
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //27
        instructs.Add(collectInstr[22]);
        // get sakuma id
        instructs.Add(getStats[3]); //29 
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,1));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); //36
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]);
        instructs.Add(Instruction.Create(OpCodes.Or)); //38
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //39
        instructs.Add(collectInstr[22]);
        // get ram id
        instructs.Add(getStats[3]); //41
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,2));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 48
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]);
        instructs.Add(Instruction.Create(OpCodes.Or)); // //50
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //51
        instructs.Add(collectInstr[22]);
        // get ram id
        instructs.Add(getStats[3]); //53
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,3));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 60
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]); 
        instructs.Add(Instruction.Create(OpCodes.Or)); // 61
        instructs.Add(brtrue[6]); // 62
        // Operand is adjusted later


        // get collectible
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[7]);
        // load 
        instructs.Add(Instruction.Create(OpCodes.Stloc_1));
        // set xpLevel
        instructs.Add(Instruction.Create(OpCodes.Ldloc_1));
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 53));
        instructs.Add(collectInstr[16]);


        // get dictionary for ::setItem() (arg0) 
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        // get warrior Id for ::setItem() (arg1)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[22]);
        // get collectibleWarrior for ::setItem() (arg2)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_1));
        //call ::setItem()
        instructs.Add(collectInstr[24]);


        //increment counter
        instructs.Add(cheatInstr[18]);
        instructs[62].Operand = instructs[instructs.Count - 1];

        instructs.Add(cheatInstr[19]);
        instructs.Add(cheatInstr[20]);
        instructs.Add(cheatInstr[21]);
        instructs.Add(cheatInstr[22]);

        // compare counter with size of list
        instructs.Add(cheatInstr[23]);
        instructs.Add(cheatInstr[24]);
        instructs.Add(cheatInstr[25]);
        // if counter < list.size, go again
        instructs.Add(cheatInstr[26]);
        var bltTarget = instructs[instructs.Count-1].Operand as Instruction;
        bltTarget = instructs[10];
        instructs[instructs.Count-1].Operand = bltTarget = bltTarget;
        
        //instructs.Add(Instruction.Create(OpCodes.Pop));

        //store dictionary
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        instructs.Add(instructs[2]);
        instructs.Add(Instruction.Create(OpCodes.Ret));
    
        increaseMaxStack(ctordata.Body, 0, 22);
    }
    static void getAll() {
        var controller = assembly.MainModule.GetType("GachaController");
        var getStats = controller.Methods.First(m=>m.Name == "SetStarterPackWarrior").Body.Instructions;

        var btru = assembly.MainModule.GetType("ElementalShiftSpecialSettings");
        var brtrue = btru.Methods.First(m=>m.Name == "PushSettingsToMechanic").Body.Instructions;

        var Wcheats = assembly.MainModule.GetType("WarriorConsoleCheats");
        var strings = Wcheats.Methods.First(m=>m.Name == "AwardWarrior").Body.Instructions;

        var playStats = assembly.MainModule.GetType("PlayerStats");
        var startPack = playStats.Methods.First(m=>m.Name == "get_SelectedStarterPack").Body;
        startPack.Instructions[12] = Instruction.Create(OpCodes.Ldc_I4, 0);
        startPack.Instructions.RemoveAt(11);
        startPack.Instructions.RemoveAt(10);
        startPack.Instructions.RemoveAt(9);
        startPack.Instructions.RemoveAt(8);
        increaseMaxStack(startPack, 6, 10);

        var cheats = assembly.MainModule.GetType("GachaCheats");
        var cheat = cheats.Methods.First(m=>m.Name == "DrawManualWarriorGetter");
        var cheatInstr = cheat.Body.Instructions;
        
        var warriors = assembly.MainModule.GetType("WarriorCollectionSaveGame");
        var data = warriors.NestedTypes.First(m=>m.Name == "Data");
        var dictionary = data.Fields.First(m=>m.Name == "_CollectionData");
        var collect = warriors.Methods.First(m=>m.Name == "CollectibleCollected");
        var collectInstr = collect.Body.Instructions;
        var ctordata = data.Methods.First(m=>m.Name == ".ctor");

        ctordata.Body.InitLocals = true;
        addLocal(ctordata.Body, localType(cheat.Body,0));
        addLocal(ctordata.Body, localType(collect.Body,0));
        addLocal(ctordata.Body, localType(cheat.Body,2));
        addLocal(ctordata.Body, dictionary.FieldType);

        var instructs = ctordata.Body.Instructions;
        instructs.RemoveAt(instructs.Count - 1);

        // store dictionary locally
        instructs.RemoveAt(instructs.Count - 1);
        instructs.Add(Instruction.Create(OpCodes.Stloc_3));

    
        // initialise counter (index 52)
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(Instruction.Create(OpCodes.Stloc_0));

        //jumpt to comparison
        //instructs.Add(cheatInstr[10]);

        // get collectibleStats at counter 
        instructs.Add(cheatInstr[12]); //10
        instructs.Add(cheatInstr[13]);
        instructs.Add(cheatInstr[14]);
        instructs.Add(cheatInstr[15]);
        // store collectibeStats
        instructs.Add(Instruction.Create(OpCodes.Stloc_2)); //15

        /*
        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //16
        instructs.Add(collectInstr[22]); 
        // get abu id
        instructs.Add(getStats[3]); //18
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,0));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 25
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]); //26
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //27
        instructs.Add(collectInstr[22]);
        // get sakuma id
        instructs.Add(getStats[3]); //29 
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,1));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); //36
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]);
        instructs.Add(Instruction.Create(OpCodes.Or)); //38
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //39
        instructs.Add(collectInstr[22]);
        // get ram id
        instructs.Add(getStats[3]); //41
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,2));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 48
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]);
        instructs.Add(Instruction.Create(OpCodes.Or)); // //50
        // result on stack for or

        // warrior id
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2)); //51
        instructs.Add(collectInstr[22]);
        // get ram id
        instructs.Add(getStats[3]); //53
        instructs.Add(getStats[4]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        instructs.Add(getStats[8]);
        instructs.Add(getStats[9]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4,3));
        instructs.Add(getStats[13]);
        instructs.Add(collectInstr[22]); // 60
        // put abu Id == warrior id onto stack
        instructs.Add(strings[14]); 
        instructs.Add(Instruction.Create(OpCodes.Or)); // 61
        instructs.Add(brtrue[6]); // 62
        // Operand is adjusted later

        */

        // get collectible
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[7]);
        // load 
        instructs.Add(Instruction.Create(OpCodes.Stloc_1));
        // set xpLevel
        instructs.Add(Instruction.Create(OpCodes.Ldloc_1));

        // int param
        //instructs.Add(collectInstr[9]);
        //instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        //instructs.Add(collectInstr[12]);
        instructs.Add(Instruction.Create(OpCodes.Ldc_I4, 0));

        // set lvl
        instructs.Add(collectInstr[16]);

        // evolve
        //instructs.Add(Instruction.Create(OpCodes.Ldloc_1));
        //instructs.Add(collectInstr[18]);

        // get dictionary for ::setItem() (arg0) 
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        // get warrior Id for ::setItem() (arg1)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_2));
        instructs.Add(collectInstr[22]);
        // get collectibleWarrior for ::setItem() (arg2)
        instructs.Add(Instruction.Create(OpCodes.Ldloc_1));
        //call ::setItem()
        instructs.Add(collectInstr[24]);


        //increment counter
        instructs.Add(cheatInstr[18]);
        //instructs[62].Operand = instructs[instructs.Count - 1];

        instructs.Add(cheatInstr[19]);
        instructs.Add(cheatInstr[20]);
        instructs.Add(cheatInstr[21]);
        instructs.Add(cheatInstr[22]);

        // compare counter with size of list
        instructs.Add(cheatInstr[23]);
        instructs.Add(cheatInstr[24]);
        instructs.Add(cheatInstr[25]);
        // if counter < list.size, go again
        instructs.Add(cheatInstr[26]);
        var bltTarget = instructs[instructs.Count-1].Operand as Instruction;
        bltTarget = instructs[10];
        instructs[instructs.Count-1].Operand = bltTarget = bltTarget;
        
        //instructs.Add(Instruction.Create(OpCodes.Pop));

        //store dictionary
        instructs.Add(Instruction.Create(OpCodes.Ldloc_3));
        instructs.Add(instructs[2]);
        instructs.Add(Instruction.Create(OpCodes.Ret));
    
        increaseMaxStack(ctordata.Body, 0, 22);
    }

    static void initOpps(float factor) {
        float foe_fact = 0.3f;
        float friend_fact = 3.0f;
        var controller = assembly.MainModule.GetType("");
        var getStats = controller.Methods.First(m=>m.Name == "InitialiseFromOpponentTemplate").Body.Instructions;

        
    }

    static void freeHurry() {
        var controller = assembly.MainModule.GetType("CollectibleWarrior");
        var getStats = controller.Methods.First(m=>m.Name == "get_NextHurryCost").Body.Instructions;
        getStats.Clear();
        getStats.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
        getStats.Add(Instruction.Create(OpCodes.Ret));
    }

    static void freeTraining() {
        var controller = assembly.MainModule.GetType("WarriorTrainingSetupScreen");
        var call = controller.Methods.First(m=>m.Name == "StartTrainingImpl");
        var getStats = controller.Methods.First(m=>m.Name == "StartTraining").Body.Instructions;
        getStats.Clear();
        getStats.Add(Instruction.Create(OpCodes.Ldarg_0));
        getStats.Add(Instruction.Create(OpCodes.Call, call));
        getStats.Add(Instruction.Create(OpCodes.Ret));
    }
    
    static void freeEvolve() {
        hasSauce();
        var controller = assembly.MainModule.GetType("EvolutionScreen");
        var call = controller.Methods.First(m=>m.Name == "StartEvolveImpl");
        var getStats = controller.Methods.First(m=>m.Name == "StartEvolution").Body.Instructions;
        getStats.Clear();
        getStats.Add(Instruction.Create(OpCodes.Ldarg_0));
        getStats.Add(Instruction.Create(OpCodes.Call, call));
        getStats.Add(Instruction.Create(OpCodes.Ret));
    }

    static void unlockMaps(int loc) {
        var meta = assembly.MainModule.GetType("MapSaveGame");
        var callUnlock = meta.Methods.First(m=>m.Name == "MapShowAsUnlocked");
        var callComplete = meta.Methods.First(m=>m.Name == "CompleteStoryMatch");

        var fake = meta.Methods.First(m=>m.Name == "CreateMapSaveGame");
        var faker = fake.Body.Instructions;
        for(int i = 0; i < loc; i++) {
            faker.Insert(24 + (7*i), Instruction.Create(OpCodes.Ldarg_0));
            faker.Insert(25 + (7*i), Instruction.Create(OpCodes.Ldc_I4, i));
            faker.Insert(26 + (7*i), Instruction.Create(OpCodes.Call, callUnlock));
            faker.Insert(27 + (7*i), Instruction.Create(OpCodes.Ldarg_0));
            faker.Insert(28 + (7*i), Instruction.Create(OpCodes.Ldc_I4, i));
            faker.Insert(29 + (7*i), Instruction.Create(OpCodes.Ldc_I4, i));
            faker.Insert(30 + (7*i), Instruction.Create(OpCodes.Call, callComplete));
        }
    }
    static void completeMaps() {
        //int[] size = {7, 9, 14, 12, 13};
        var meta = assembly.MainModule.GetType("MapSaveGame");
        var complete = meta.Methods.First(m=>m.Name == "IsLocationComplete").Body.Instructions;
        //var complete = meta.Methods.First(m=>m.Name == "IsStoryMatchComplete").Body.Instructions;
        complete.Clear();
        complete.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
        complete.Add(Instruction.Create(OpCodes.Ret));

        //complete = meta.Methods.First(m=>m.Name == "IsLocationShownAsComplete").Body.Instructions;
        //complete = meta.Methods.First(m=>m.Name == "IsStoryMatchShownAsComplete").Body.Instructions;
        //complete.Clear();
        //complete.Add(Instruction.Create(OpCodes.Ldc_I4, 1));
        //complete.Add(Instruction.Create(OpCodes.Ret));
    }

    static void enterTower() {
        var meta = assembly.MainModule.GetType("LeaderboardService");
        var fake = meta.Methods.First(m=>m.Name == "get_FakeMetaData");
        fake.Attributes &= ~MethodAttributes.Private;

        var controller = assembly.MainModule.GetType("TowerController");
        var getStats = controller.Methods.First(m=>m.Name == "TriggerGenericEvent").Body.Instructions;
        getStats.RemoveAt(79);
        getStats.RemoveAt(79);
        getStats.RemoveAt(79);
        getStats.Insert(79, Instruction.Create(OpCodes.Ldc_I4, 1));
        
    }

    static void modDifficulty(float factor) {
        var meta = assembly.MainModule.GetType("TowerMapSideBar");
        var fake = meta.Methods.First(m=>m.Name == "Initialise").Body.Instructions;
        fake[70].Operand = factor;
    }

    static void maxStamina(int factor) {
        var meta = assembly.MainModule.GetType("PlayerStats");
        var fake = meta.Methods.First(m=>m.Name == "get_MaxStamina").Body.Instructions;
        var data = meta.NestedTypes.FirstOrDefault(n => n.Name == "PlayerStatsData");
        var dataCon = data.Methods.First(m => m.Name == ".ctor").Body;

        fake.Clear();
        fake.Add(Instruction.Create(OpCodes.Ldc_I4, factor));
        fake.Add(dataCon.Instructions[17]);
        fake.Add(Instruction.Create(OpCodes.Ret));

    }
    
    static void oppStats(float factor) {
        var meta = assembly.MainModule.GetType("PlayerToOpponentStatsConverter");
        var fake = meta.Methods.First(m=>m.Name == "GetOpponentStats").Body.Instructions;
        fake.Insert(fake.Count-2, Instruction.Create(OpCodes.Ldc_R4, factor));
                fake.Insert(fake.Count-2, Instruction.Create(OpCodes.Mul));
        fake.Insert(35, Instruction.Create(OpCodes.Ldc_R4, factor));
                fake.Insert(36, Instruction.Create(OpCodes.Mul));
    }

    static void modPrizeGen() {
        var meta = assembly.MainModule.GetType("GachaTable");
        var fake = meta.Methods.First(m=>m.Name == "GeneratePrize").Body.Instructions;
        
    }

    public static MethodDefinition addMethod(TypeDefinition typeDefinition, MethodDefinition sourceMethodDefinition, string methodName)
    {
        // 1. Create a new MethodDefinition with the specified name
        var methodReturnType = sourceMethodDefinition.ReturnType; // Use the return type of the source method
        var method = new MethodDefinition(methodName, sourceMethodDefinition.Attributes, methodReturnType);

        // 2. Copy parameters from the source method to the new method (by reference)
        foreach (var sourceParam in sourceMethodDefinition.Parameters)
        {
            // Directly add parameter by reference
            var clonedParam = new ParameterDefinition(sourceParam.Name, sourceParam.Attributes, sourceParam.ParameterType)
            {
                Constant = sourceParam.Constant // Copy the constant value if there is one
            };
            method.Parameters.Add(clonedParam);
        }

        // 3. Copy generic parameters if the source method is generic (by reference)
        if (sourceMethodDefinition.HasGenericParameters)
        {
            foreach (var sourceGenericParam in sourceMethodDefinition.GenericParameters)
            {
                // Directly add generic parameter by reference
                var clonedGenericParam = new GenericParameter(sourceGenericParam.Name, method);
                method.GenericParameters.Add(clonedGenericParam);
            }
        }

        // 4. Clone the method body (except the instructions) from the source method
        var body = method.Body;
        var ilProcessor = body.GetILProcessor();

        // 5. Clone local variables (by reference)
        foreach (var sourceLocal in sourceMethodDefinition.Body.Variables)
        {
            var clonedLocal = new VariableDefinition(sourceLocal.VariableType);
            body.Variables.Add(clonedLocal);
        }

        // 6. Clone the instructions to avoid passing by reference
        

        // 7. Add the new method to the type's methods collection
        typeDefinition.Methods.Add(method);

        // 8. Return the newly created method definition
        return method;
    }

    static void modTower() {
        var met = assembly.MainModule.GetType("ListExtension");
        var source = met.Methods.First(m=>m.Name == "RandomOneAndRemove");
        var src = source.Body.Instructions;
        var opps = addMethod(met, source, "tower");
        var ops = opps.Body.Instructions;
        for(int i = 0; i < src.Count; i++) {
            ops.Add(src[i]);
        }
        opps.Body.InitLocals = true;
        appendForStack(opps, 5);
        var meta = assembly.MainModule.GetType("TowerMapSideBar");
        var fake = meta.Methods.First(m=>m.Name == "Initialise").Body.Instructions;
        fake[65] = Instruction.Create(OpCodes.Call, opps);
        //if (fake[65].Operand is MethodReference methodRef)
      //fake.Insert(65, Instruction.Create(OpCodes.Callvirt, source));
    }

    static void startingPack(int index) {
        var playStats = assembly.MainModule.GetType("PlayerStats");
        var startPack = playStats.Methods.First(m=>m.Name == "get_SelectedStarterPack").Body;
        startPack.Instructions[12] = Instruction.Create(OpCodes.Ldc_I4, index);
        startPack.Instructions.RemoveAt(11);
        startPack.Instructions.RemoveAt(10);
        startPack.Instructions.RemoveAt(9);
        startPack.Instructions.RemoveAt(8);
        increaseMaxStack(startPack, 6, 10);
    }
    

    static void Main()
    {
        printMethods("GachaCheats", "AddRandomWarriorToSaveGame");
        removeTutorials();
        triggerGacha();
        //gachaByIndex();
        gachaScreen();
        setIdentity();
        setPlayerStats();
        //setStarting();
        //getWarriorsMaxed();
        //hackEvolve();
        getAll();
        freeHurry();
        freeTraining();
        freeEvolve();
        enterTower();
        completeMaps();
        //oppStats(0.1f);
        maxStamina(1000);
        //modDifficulty(0.000001f);
        //modTower();
        assembly.Write("modified_assembly.dll");
        // Load the assembly
 
    }
}

