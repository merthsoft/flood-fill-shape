using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

[HarmonyPatch(typeof(DesignatorManager), nameof(DesignatorManager.ProcessInputEvents))]
public static class FloodFillConfirmation
{
    public static bool IsDestructive(Designator designator) => designator is Designator_Cancel or Designator_Plan_Remove
        or Designator_Mine or Designator_Deconstruct or Designator_PlantsCut or Designator_RemoveFloor;

    public static bool Prefix(DesignatorManager __instance)
    {
        var e = Event.current;
        var style = __instance.SelectedStyle;
        var tool = __instance.SelectedDesignator;
        if (!FloodFillMod.Settings.ConfirmDestructiveFloodFill || style?.DrawStyleWorker is not Fill
            || !IsDestructive(tool) || e == null || e.type != EventType.MouseUp || e.button != 0
            || !__instance.Dragger.Dragging)
            return true;

        var owner = NativeFillOwnership.Current();
        if (owner == FillCellOwner.Integrated) return true;
        if (owner == FillCellOwner.Unknown)
        {
            __instance.Dragger.EndDrag();
            e.Use();
            Messages.Message("Merthsoft_FloodFill_OwnershipUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        var map = Find.CurrentMap;
        if (map == null || !UI.MouseCell().InBounds(map))
        {
            __instance.Dragger.EndDrag();
            e.Use();
            return false;
        }

        var seed = UI.MouseCell();
        var cells = __instance.Dragger.DragCells.Where(c => c.InBounds(map) && tool.CanDesignateCell(c).Accepted)
            .Distinct().ToArray();
        __instance.Dragger.EndDrag();
        e.Use();
        if (cells.Length == 0) return false;

        var pending = new PendingFloodFillAction(
            () => Find.CurrentMap == map && __instance.SelectedDesignator == tool && __instance.SelectedStyle == style
                && NativeFillOwnership.Current() == FillCellOwner.Native,
            () =>
            {
                var allowed = FloodFill.Fill(seed).ToHashSet();
                var accepted = cells.Where(c => allowed.Contains(c) && c.InBounds(map) && tool.CanDesignateCell(c).Accepted).ToArray();
                if (accepted.Length > 0) tool.DesignateMultiCell(accepted);
            });
        Find.WindowStack.Add(new Dialog_MessageBox(
            "Merthsoft_FloodFill_ConfirmDestructiveText".Translate(tool.LabelCap, cells.Length),
            buttonAText: "Merthsoft_FloodFill_ConfirmApply".Translate(), buttonAAction: pending.Confirm,
            buttonBText: "Merthsoft_FloodFill_ConfirmKeep".Translate(), buttonBAction: pending.Cancel,
            buttonADestructive: true, cancelAction: pending.Cancel));
        return false;
    }
}
