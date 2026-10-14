using Merthsoft.DesignatorShapes.FloodFill;
using RimWorld;
using UnityEngine;
using Verse;

static class FeatureChecks
{
    static IntVec3 P(int x) => new(x, 0, 0);
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
    public static void Run()
    {
        var settings = FloodFillMod.Settings;
        Check(!settings.EnableCustomFloodFillBarriers && !settings.ConfirmDestructiveFloodFill, "barriers and confirmation default off");
        var table = new ThingDef { defName = "Table" };
        var map = new Map(); Find.CurrentMap = map;
        map.thingGrid.cells[P(1)] = new() { new Thing { def = table } };
        settings.AdditionalFloodFillBarrierDefs.Add(table.defName);
        Check(FloodFill.Fill(P(0)).Count() == 4, "disabled additional barriers preserve traversal");
        settings.EnableCustomFloodFillBarriers = true;
        Check(FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[] { P(0) }), "selected building blocks region and hidden connectivity");
        map.designationManager.cells.UnionWith(new[] { P(0), P(1), P(2) });
        Check(FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[] { P(0) }), "selected building blocks designated region");
        foreach (var name in new[] { "Blueprint", "Frame" })
        {
            map.thingGrid.cells[P(1)] = new() { new Thing { def = new ThingDef { defName = name, entityDefToBuild = table } } };
            Check(FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[] { P(0) }), name + " resolves selected target barrier");
        }
        var wall = new ThingDef { defName = "Wall", designationCategory = new() { defName = "Structure" } };
        settings.AdditionalFloodFillBarrierDefs.Add(wall.defName);
        map = new Map(); Find.CurrentMap = map;
        map.thingGrid.cells[P(0)] = new() { new Thing { def = wall } }; map.thingGrid.cells[P(1)] = new() { new Thing { def = wall } };
        Check(FloodFill.Fill(P(0)).Count() == 2, "selected seed wall type remains intentionally fillable");
        settings.AdditionalFloodFillBarrierDefs.Add("Ore");
        var ore = new ThingDef { defName = "Ore" };
        map.thingGrid.cells[P(0)] = new() { new Mineable { def = ore } }; map.thingGrid.cells[P(1)] = new() { new Mineable { def = ore } };
        Check(FloodFill.Fill(P(0)).Count() == 2, "selected seed ore type remains intentionally fillable");
        settings.EnableCustomFloodFillBarriers = false;

        DesignatorManager Setup(Designator tool)
        {
            Find.CurrentMap = new Map();
            var dm = new DesignatorManager { SelectedDesignator = tool, SelectedStyle = new() { DrawStyleWorker = new Fill() } };
            Find.DesignatorManager = dm; dm.Dragger.DragCells.AddRange(new[] { P(0), P(1) });
            Find.WindowStack.dialogs.Clear(); Event.current = new() { type = EventType.MouseUp }; UI.Mouse = P(0);
            return dm;
        }
        settings.ConfirmDestructiveFloodFill = true;
        foreach (var tool in new Designator[] { new Designator_Cancel(), new Designator_Plan_Remove(), new Designator_Mine(), new Designator_MineVein(), new Designator_Deconstruct(), new Designator_PlantsCut(), new Designator_RemoveFloor() })
            Check(FloodFillConfirmation.IsDestructive(tool), "destructive whitelist " + tool.GetType().Name);
        foreach (var tool in new Designator[] { new Designator_Harvest(), new Designator_ZoneAdd(), new Designator() })
            Check(FloodFillConfirmation.Prefix(Setup(tool)) && Find.WindowStack.dialogs.Count == 0, "no confirmation for " + tool.GetType().Name);
        var cancelTool = new Designator_Cancel(); var manager = Setup(cancelTool);
        Check(!FloodFillConfirmation.Prefix(manager) && cancelTool.applied.Count == 0 && !manager.Dragger.Dragging && Event.current.used, "commit intercepted atomically and drag cleaned up");
        var dialog = Find.WindowStack.dialogs.Single(); dialog.cancel(); dialog.confirm();
        Check(cancelTool.applied.Count == 0 && manager.SelectedDesignator == cancelTool, "cancel applies nothing and preserves tool despite repeated callbacks");
        manager = Setup(cancelTool); FloodFillConfirmation.Prefix(manager); dialog = Find.WindowStack.dialogs.Single();
        manager.Dragger.DragCells.Clear(); cancelTool.rejected.Add(P(1)); dialog.confirm(); dialog.confirm();
        Check(cancelTool.applied.SequenceEqual(new[] { P(0) }), "confirm uses snapshot revalidates acceptance and commits exactly once");
        Check(FloodFillConfirmation.Prefix(manager) && Find.WindowStack.dialogs.Count == 1, "same release cannot prompt twice after EndDrag");
        foreach (var context in new[] { "map", "tool", "style" })
        {
            var tool = new Designator_Cancel(); manager = Setup(tool); FloodFillConfirmation.Prefix(manager); dialog = Find.WindowStack.dialogs.Single();
            if (context == "map") Find.CurrentMap = new Map();
            if (context == "tool") manager.SelectedDesignator = new Designator_Cancel();
            if (context == "style") manager.SelectedStyle = new() { DrawStyleWorker = new Fill() };
            dialog.confirm(); Check(tool.applied.Count == 0, "changed " + context + " invalidates confirmation");
        }
        var blockedTool = new Designator_Cancel(); manager = Setup(blockedTool); FloodFillConfirmation.Prefix(manager);
        dialog = Find.WindowStack.dialogs.Single();
        settings.RestrictFloodFillToRevealedCells = true; Find.CurrentMap.fogged.Add(P(1)); dialog.confirm();
        Check(blockedTool.applied.SequenceEqual(new[] { P(0) }), "confirmation rechecks current fog and fill eligibility");
        settings.RestrictFloodFillToRevealedCells = false;
        manager = Setup(new Designator_Cancel()); Event.current.type = EventType.Repaint;
        Check(FloodFillConfirmation.Prefix(manager) && Find.WindowStack.dialogs.Count == 0, "preview repaint never prompts");
        Event.current.type = EventType.MouseUp; manager.SelectedStyle = null;
        Check(FloodFillConfirmation.Prefix(manager), "unrelated style bypasses hook");
        manager = Setup(new Designator_Cancel()); UI.Mouse = P(-1);
        Check(!FloodFillConfirmation.Prefix(manager) && !manager.Dragger.Dragging && Find.WindowStack.dialogs.Count == 0, "off-map release safely abandons batch");
        manager = Setup(new Designator_Cancel()); manager.Dragger.DragCells.RemoveAt(1);
        Check(!FloodFillConfirmation.Prefix(manager) && Find.WindowStack.dialogs.Count == 1, "single-cell click release prompts once");
        Find.WindowStack.dialogs.Single().cancel(); settings.ConfirmDestructiveFloodFill = false;
        manager = Setup(new Designator_Cancel());
        Check(FloodFillConfirmation.Prefix(manager) && Find.WindowStack.dialogs.Count == 0, "disabled confirmation preserves vanilla commit path");
    }
}
