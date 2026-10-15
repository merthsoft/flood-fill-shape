// Isolated process only: loads installed game libraries, never connects to the running game.
using System.Reflection;
using System.Runtime.Loader;
if (args.Length != 2) throw new ArgumentException("Pass RimWorld directory and rebuilt mod DLL.");
var gameDir = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    var path = Path.Combine(gameDir, name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameDir, "Assembly-CSharp.dll"));
var mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
var current = game.GetType("Verse.Current")!;
var state = game.GetType("Verse.ProgramState")!;
current.GetProperty("ProgramState")!.SetValue(null, Enum.Parse(state, "Playing"));
var text = Activator.CreateInstance(game.GetType("Verse.TaggedString")!, new object[] { "Test confirmation" });
var type = mod.GetType("Merthsoft.DesignatorShapes.FloodFill.FloodFillConfirmationDialog")!;
int applied = 0, cancelled = 0;
var dialog = Activator.CreateInstance(type, new object[] { text!, (Action)(() => applied++), (Action)(() => cancelled++) })!;
var layer = game.GetType("Verse.Window")!.GetField("layer")!;
if (layer.GetValue(dialog)!.ToString() != "Dialog") throw new Exception("Initial dialog ordering changed");
// Without a game singleton, entering Window.PreOpen's dialog-layer Deselect path also fails.
// This exercises real sizing/initialization while deliberately avoiding that reset.
type.GetMethod("PreOpen")!.Invoke(dialog, null);
if (layer.GetValue(dialog)!.ToString() != "Dialog") throw new Exception("Dialog layer was not restored");
((Action)type.GetField("buttonBAction")!.GetValue(dialog)!)();
if (applied != 0 || cancelled != 1) throw new Exception("Cancel mutated the action");
Console.WriteLine("PASS real 1.7 dialog PreOpen under isolated Playing state preserves Dialog layer and cancellation callback");
Console.WriteLine("Actual WindowStack, selected tool identity, and interaction still require in-game verification.");
