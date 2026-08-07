using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

// =============================================================================
// ModHarness — proves Oink mod Inject() CODE executes WITHOUT launching game.
// =============================================================================

string repoRoot = FindRepoRoot(AppContext.BaseDirectory);
string oinkDllPath = Path.Combine(repoRoot, "mods", "Oink", "src", "Oink", "bin", "Release", "net40", "Oink.dll");
string xnaLibDir = Path.Combine(repoRoot, "mods", "Oink", "lib", "xna");
string gameContentDir = Path.Combine(repoRoot, "game", "Blood and Bacon", "Content");
string harnessDir = AppContext.BaseDirectory;

int passCount = 0;
int failCount = 0;

void Pass(string label)
{
    passCount++;
    Console.WriteLine($"PASS: {label}");
}

void Fail(string label, string detail)
{
    failCount++;
    Console.WriteLine($"FAIL: {label} — {detail}");
}

// ---------------------------------------------------------------------------
// Step 0: Setup harness base directory — copy config files so OinkConfig.Load
//         finds Content\Oink\config.txt (and also oink.txt as fallback).
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 0: Setup config files ===");

string configSrc = Path.Combine(repoRoot, "mods", "Oink", "config.txt");

string contentOinkDir = Path.Combine(harnessDir, "Content", "Oink");
Directory.CreateDirectory(contentOinkDir);
string configInContent = Path.Combine(contentOinkDir, "config.txt");
File.Copy(configSrc, configInContent, overwrite: true);
string configAsOinkTxt = Path.Combine(harnessDir, "oink.txt");
File.Copy(configSrc, configAsOinkTxt, overwrite: true);

if (File.Exists(configInContent) && File.Exists(configAsOinkTxt))
    Pass("Config files copied to Content\\Oink\\config.txt + oink.txt");
else
    Fail("Config copy", "Files missing after copy");

// Delete stale oink.log so we can verify fresh write
string oinkLogPath = Path.Combine(harnessDir, "oink.log");
if (File.Exists(oinkLogPath)) File.Delete(oinkLogPath);

// ---------------------------------------------------------------------------
// Step 1: Load Oink.dll via reflection + XNA assemblies
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 1: Load Oink.dll + XNA assemblies ===");

Assembly? oinkAsm = null;
Assembly? xnaFrameworkAsm = null;
Assembly? xnaGameAsm = null;

try
{
    // Load XNA assemblies first (Oink.dll references them)
    xnaFrameworkAsm = Assembly.LoadFrom(Path.Combine(xnaLibDir, "Microsoft.Xna.Framework.dll"));
    Console.WriteLine($"  Loaded: {xnaFrameworkAsm.GetName().FullName}");

    Assembly.LoadFrom(Path.Combine(xnaLibDir, "Microsoft.Xna.Framework.Graphics.dll"));
    Console.WriteLine("  Loaded: Microsoft.Xna.Framework.Graphics");

    Assembly.LoadFrom(Path.Combine(xnaLibDir, "Microsoft.Xna.Framework.Storage.dll"));
    Console.WriteLine("  Loaded: Microsoft.Xna.Framework.Storage");

    // Input.Touch comes from GAC (MSIL, not in lib\xna)
    string touchPath = Path.Combine(harnessDir, "Microsoft.Xna.Framework.Input.Touch.dll");
    Assembly.LoadFrom(touchPath);
    Console.WriteLine("  Loaded: Microsoft.Xna.Framework.Input.Touch (from GAC copy)");

    xnaGameAsm = Assembly.LoadFrom(Path.Combine(xnaLibDir, "Microsoft.Xna.Framework.Game.dll"));
    Console.WriteLine($"  Loaded: {xnaGameAsm.GetName().FullName}");

    // Now load Oink.dll
    oinkAsm = Assembly.LoadFrom(oinkDllPath);
    Console.WriteLine($"  Loaded: {oinkAsm.GetName().FullName}");

    Pass("Oink.dll + all XNA references loaded");
}
catch (Exception ex)
{
    Fail("Assembly load", ex.ToString());
    Environment.Exit(1);
}

// ---------------------------------------------------------------------------
// Step 2: Verify OinkEntry type + Inject method exist
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 2: Resolve OinkEntry.Inject method ===");

Type? oinkEntryType = oinkAsm!.GetType("Oink.OinkEntry");
MethodInfo? injectMethod = null;
PropertyInfo? enabledProp = null;

if (oinkEntryType == null)
{
    Fail("Oink.OinkEntry type", "Type not found in Oink.dll");
    Environment.Exit(1);
}
else
{
    Pass("Oink.OinkEntry type resolved");

    injectMethod = oinkEntryType.GetMethod("Inject", BindingFlags.Public | BindingFlags.Static);
    if (injectMethod != null)
        Pass("Inject(Game) method resolved");
    else
        Fail("Inject method", "Method not found");

    enabledProp = oinkEntryType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
    if (enabledProp != null)
        Pass("Enabled property getter resolved");
    else
        Fail("Enabled property", "Property not found");
}

// ---------------------------------------------------------------------------
// Step 3: Construct ShimGame (XNA Game subclass) headlessly
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 3: Construct ShimGame ===");

Type? gameType = xnaGameAsm!.GetType("Microsoft.Xna.Framework.Game", throwOnError: true);
Console.WriteLine($"  Game type: {gameType!.FullName}  abstract={gameType.IsAbstract}");

object? shimGame = null;
bool gameConstructed = false;

try
{
    // Build dynamic subclass of Game
    var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ShimAsm"), AssemblyBuilderAccess.Run);
    var mb = ab.DefineDynamicModule("Shim");
    var tb = mb.DefineType("ShimGame", TypeAttributes.Public | TypeAttributes.Class, gameType);
    var ctorBase = gameType.GetConstructor(Type.EmptyTypes);
    var cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
    var il = cb.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Call, ctorBase!);
    il.Emit(OpCodes.Ret);
    var shimType = tb.CreateType();

    shimGame = Activator.CreateInstance(shimType);
    gameConstructed = shimGame != null;

    if (gameConstructed)
    {
        Pass("ShimGame constructed (XNA Game ctor ran headlessly)");
        Console.WriteLine($"  ShimGame type: {shimGame!.GetType().FullName}");

        // Verify Components collection exists
        var components = gameType.GetProperty("Components")!.GetValue(shimGame);
        Console.WriteLine($"  Components collection: {components?.GetType().FullName}");
        if (components != null)
            Pass("Game.Components accessible");
        else
            Fail("Game.Components", "Components is null");

        // Verify Content exists
        var content = gameType.GetProperty("Content")!.GetValue(shimGame);
        Console.WriteLine($"  Content manager: {content?.GetType().FullName}");
        if (content != null)
            Pass("Game.Content accessible");
        else
            Fail("Game.Content", "Content is null");
    }
    else
    {
        Fail("ShimGame construction", "Activator returned null");
    }
}
catch (Exception ex)
{
    Fail("ShimGame construction", FormatException(ex));
    Console.WriteLine("\n  Falling back: will test component-add via direct GameComponentCollection");
}

// ---------------------------------------------------------------------------
// Step 4: Invoke OinkEntry.Inject(shimGame)
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 4: Invoke OinkEntry.Inject(shimGame) ===");

if (gameConstructed && shimGame != null && injectMethod != null)
{
    try
    {
        injectMethod.Invoke(null, new object?[] { shimGame });
        Pass("Inject() did NOT throw");
    }
    catch (Exception ex)
    {
        Fail("Inject() execution", FormatException(ex));
    }

    // Step 4a: Verify OinkHook component was added
    Console.WriteLine("\n=== Step 4a: Verify OinkHook in shimGame.Components ===");

    try
    {
        var components = (IEnumerable)gameType.GetProperty("Components")!.GetValue(shimGame)!;
        var componentList = new List<object>();
        foreach (var c in components) componentList.Add(c);

        Console.WriteLine($"  Components count: {componentList.Count}");
        foreach (var c in componentList)
            Console.WriteLine($"    -> {c.GetType().FullName}  UpdateOrder={c.GetType().GetProperty("UpdateOrder")?.GetValue(c)}");

        int hookCount = componentList.Count(c => c.GetType().FullName == "Oink.OinkHook");
        if (hookCount == 1)
            Pass("Exactly one Oink.OinkHook component in Components");
        else
            Fail($"Expected 1 OinkHook, found {hookCount}", $"Total components: {componentList.Count}");

        // Verify UpdateOrder == int.MaxValue
        var hook = componentList.FirstOrDefault(c => c.GetType().FullName == "Oink.OinkHook");
        if (hook != null)
        {
            var uo = hook.GetType().GetProperty("UpdateOrder")?.GetValue(hook);
            if (uo is int orderVal && orderVal == int.MaxValue)
                Pass("OinkHook.UpdateOrder == int.MaxValue");
            else
                Fail("OinkHook.UpdateOrder", $"Got {uo}, expected {int.MaxValue}");
        }
    }
    catch (Exception ex)
    {
        Fail("Components enumeration", FormatException(ex));
    }
}
else
{
    Console.WriteLine("  SKIPPED: ShimGame was not constructed");
}

// ---------------------------------------------------------------------------
// Step 5: Verify oink.log was written with "Oink injected."
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 5: Verify oink.log ===");

if (File.Exists(oinkLogPath))
{
    string logContent = File.ReadAllText(oinkLogPath, Encoding.UTF8);
    Console.WriteLine("  oink.log contents:");
    foreach (var line in logContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        Console.WriteLine($"    | {line.TrimEnd('\r')}");

    if (logContent.Contains("Oink injected."))
        Pass("oink.log contains \"Oink injected.\"");
    else
        Fail("oink.log content", "Missing \"Oink injected.\" line");

    if (logContent.Contains("ScreenManager not found"))
        Pass("oink.log logs ScreenManager not found (expected — no real ScreenManager in shim)");
    else
        Console.WriteLine("  NOTE: ScreenManager log line not found");
}
else
{
    Fail("oink.log existence", "File not created");
}

// ---------------------------------------------------------------------------
// Step 6: Invoke OinkHook.Update(GameTime) via reflection once
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 6: Invoke OinkHook.Update(GameTime) ===");

if (gameConstructed && shimGame != null)
{
    try
    {
        var components = (IEnumerable)gameType.GetProperty("Components")!.GetValue(shimGame)!;
        object? hook = null;
        foreach (var c in components)
        {
            if (c.GetType().FullName == "Oink.OinkHook") { hook = c; break; }
        }

        if (hook != null)
        {
            // Construct a GameTime via reflection — GameTime is in Framework.Game.dll
            Type? gameTimeType = Type.GetType("Microsoft.Xna.Framework.GameTime, Microsoft.Xna.Framework.Game")
                ?? xnaGameAsm!.GetType("Microsoft.Xna.Framework.GameTime");
            Console.WriteLine($"  GameTime type resolved: {gameTimeType?.FullName ?? "null"}");
            var gameTime = Activator.CreateInstance(gameTimeType!);

            var updateMethod = hook.GetType().GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            updateMethod!.Invoke(hook, new object?[] { gameTime });

            Pass("OinkHook.Update(GameTime) invoked without throw");
        }
        else
        {
            Fail("OinkHook.Update", "Hook not found in Components");
        }
    }
    catch (Exception ex)
    {
        Fail("OinkHook.Update() execution", FormatException(ex));
    }

    // Read oink.log again for update-time logs
    if (File.Exists(oinkLogPath))
    {
        string logContent = File.ReadAllText(oinkLogPath, Encoding.UTF8);
        var lines = logContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var updateLines = lines.Where(l => l.Contains("Hook update fired") || l.Contains("No screens") || l.Contains("Game screen") || l.Contains("enabled") || l.Contains("disabled") || l.Contains("Config load")).ToList();
        Console.WriteLine("  Update-time log lines:");
        foreach (var l in updateLines)
            Console.WriteLine($"    | {l.TrimEnd('\r')}");
        if (updateLines.Count > 0)
            Pass("Update produced log output");
        else
            Console.WriteLine("  (no update-specific log lines — may be expected if ScreenManager is null)");
    }
}
else
{
    Console.WriteLine("  SKIPPED: ShimGame was not constructed");
}

// ---------------------------------------------------------------------------
// Step 7: Invoke OinkEntry.Enabled getter
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 7: OinkEntry.Enabled getter ===");

if (enabledProp != null)
{
    try
    {
        object? enabledVal = enabledProp.GetValue(null, null);
        Console.WriteLine($"  OinkEntry.Enabled = {enabledVal}");
        Pass("Enabled getter executed without throw");
    }
    catch (Exception ex)
    {
        Fail("Enabled getter", FormatException(ex));
    }
}
else
{
    Console.WriteLine("  SKIPPED: Enabled property not resolved");
}

// ---------------------------------------------------------------------------
// Step 8: Config values from OinkConfig.Load (proves config file read)
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 8: OinkConfig.Load() values ===");

Type? oinkConfigType = oinkAsm!.GetType("Oink.OinkConfig");
if (oinkConfigType != null)
{
    var loadMethod = oinkConfigType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
    if (loadMethod != null)
    {
        try
        {
            object cfg = loadMethod.Invoke(null, null)!;
            string enabled = (string)oinkConfigType.GetField("Enabled")!.GetValue(cfg)!;
            string pigSkin = (string)oinkConfigType.GetField("PigSkin")!.GetValue(cfg)!;
            string speedMult = (string)oinkConfigType.GetField("SpeedMultiplier")!.GetValue(cfg)!;
            string toggleKey = (string)oinkConfigType.GetField("ToggleKey")!.GetValue(cfg)!;
            string pigTexture = (string)oinkConfigType.GetField("PigTexture")!.GetValue(cfg)!;

            Console.WriteLine($"  Enabled          = {enabled}");
            Console.WriteLine($"  PigSkin          = {pigSkin}");
            Console.WriteLine($"  SpeedMultiplier  = {speedMult}");
            Console.WriteLine($"  ToggleKey        = {toggleKey}");
            Console.WriteLine($"  PigTexture       = {pigTexture}");

            bool configOk = enabled == "true" && pigSkin == "true" && speedMult == "1.35"
                          && toggleKey == "O" && pigTexture == "npc/piggy1";
            if (configOk)
                Pass("Config values match config.txt");
            else
                Fail("Config values", "One or more values do not match expected config.txt");
        }
        catch (Exception ex)
        {
            Fail("Config load", FormatException(ex));
        }
    }
}

// ---------------------------------------------------------------------------
// Step 9: Verify game content texture exists (READ-ONLY check)
// ---------------------------------------------------------------------------
Console.WriteLine("\n=== Step 9: Verify game content npc\\piggy1.xnb exists (READ-ONLY) ===");

string pigTexturePath = Path.Combine(gameContentDir, "npc", "piggy1.xnb");
if (File.Exists(pigTexturePath))
{
    var fi = new FileInfo(pigTexturePath);
    Pass($"npc\\piggy1.xnb exists ({fi.Length} bytes) — NOT loaded (needs graphics device)");
}
else
{
    Fail("npc\\piggy1.xnb", $"File not found at {pigTexturePath}");
}

// ---------------------------------------------------------------------------
// Final report
// ---------------------------------------------------------------------------
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine($"RESULTS: {passCount} PASS, {failCount} FAIL");
Console.WriteLine(new string('=', 60));
Console.WriteLine("\n=== Full oink.log contents ===");
if (File.Exists(oinkLogPath))
{
    Console.WriteLine(File.ReadAllText(oinkLogPath, Encoding.UTF8).TrimEnd());
}
else
{
    Console.WriteLine("(oink.log does not exist)");
}
Console.WriteLine("\n=== Harness directory listing ===");
foreach (var f in Directory.GetFiles(harnessDir, "*", SearchOption.AllDirectories).OrderBy(f => f))
    Console.WriteLine($"  {Path.GetRelativePath(harnessDir, f)}");

Environment.Exit(failCount == 0 ? 0 : 1);

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static string FormatException(Exception ex)
{
    var sb = new StringBuilder();
    Exception? e = ex;
    while (e != null)
    {
        sb.Append(e.GetType().Name).Append(": ").Append(e.Message);
        if (e.InnerException != null) sb.Append(" → ");
        e = e.InnerException;
    }
    return sb.ToString();
}

static string FindRepoRoot(string startDir)
{
    string dir = startDir;
    for (int i = 0; i < 10; i++)
    {
        if (File.Exists(Path.Combine(dir, "Directory.Build.props")) || Directory.Exists(Path.Combine(dir, "mods")))
            return dir;
        var parent = Directory.GetParent(dir);
        if (parent == null) return startDir;
        dir = parent.FullName;
    }
    return startDir;
}
