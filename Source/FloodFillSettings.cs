using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

public class FloodFillSettings : ModSettings
{
    public bool RestrictFloodFillToRevealedCells = false;
    public bool EnableCustomFloodFillBarriers = false;
    public bool ConfirmDestructiveFloodFill = false;
    public List<string> AdditionalFloodFillBarrierDefs = new();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ConfirmDestructiveFloodFill, nameof(ConfirmDestructiveFloodFill), false);
        Scribe_Values.Look(ref EnableCustomFloodFillBarriers, nameof(EnableCustomFloodFillBarriers), false);
        Scribe_Collections.Look(ref AdditionalFloodFillBarrierDefs, nameof(AdditionalFloodFillBarrierDefs), LookMode.Value);
        AdditionalFloodFillBarrierDefs ??= new();
        Scribe_Values.Look(ref RestrictFloodFillToRevealedCells, nameof(RestrictFloodFillToRevealedCells), false);
    }
}

public class FloodFillMod : Mod
{
    public static FloodFillSettings Settings { get; private set; } = new();

    public FloodFillMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FloodFillSettings>();
        new Harmony("Merthsoft.DesignatorShapes.FloodFill.Confirmation").PatchAll();
    }

    private string barrierSearch = "";
    private Vector2 barrierScroll;

    public override string SettingsCategory() => "Flood Fill Shape";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("Merthsoft_FloodFill_Settings_RestrictToRevealedCells".Translate(),
            ref Settings.RestrictFloodFillToRevealedCells,
            "Merthsoft_FloodFill_Settings_RestrictToRevealedCellsTooltip".Translate());
        listing.CheckboxLabeled("Merthsoft_FloodFill_Settings_ConfirmDestructive".Translate(),
            ref Settings.ConfirmDestructiveFloodFill,
            "Merthsoft_FloodFill_Settings_ConfirmDestructiveTooltip".Translate());
        listing.Gap();
        listing.CheckboxLabeled("Merthsoft_FloodFill_Settings_AdditionalBarriers".Translate(),
            ref Settings.EnableCustomFloodFillBarriers,
            "Merthsoft_FloodFill_Settings_AdditionalBarriersTooltip".Translate());
        if (Settings.EnableCustomFloodFillBarriers)
        {
            listing.Label("Merthsoft_FloodFill_Settings_SearchBuildings".Translate());
            barrierSearch = listing.TextEntry(barrierSearch);
            var defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.category == ThingCategory.Building && d.entityDefToBuild == null
                    && (d.label ?? d.defName).IndexOf(barrierSearch, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(d => d.label ?? d.defName).ToList();
            var outer = listing.GetRect(240f);
            var view = new Rect(0f, 0f, outer.width - 20f, defs.Count * 28f);
            Widgets.BeginScrollView(outer, ref barrierScroll, view);
            for (var i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                var selected = Settings.AdditionalFloodFillBarrierDefs.Contains(def.defName);
                var before = selected;
                Widgets.CheckboxLabeled(new Rect(0f, i * 28f, view.width, 28f),
                    (def.LabelCap.ToString() is { Length: > 0 } label ? label : def.defName) + " (" + def.defName + ")", ref selected);
                if (selected != before)
                {
                    if (selected) Settings.AdditionalFloodFillBarrierDefs.Add(def.defName);
                    else Settings.AdditionalFloodFillBarrierDefs.RemoveAll(id => id == def.defName);
                }
            }
            Widgets.EndScrollView();
        }
        listing.End();
    }
}
