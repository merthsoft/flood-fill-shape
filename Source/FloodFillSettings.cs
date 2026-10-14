using UnityEngine;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

public class FloodFillSettings : ModSettings
{
    public bool RestrictFloodFillToRevealedCells = false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref RestrictFloodFillToRevealedCells, nameof(RestrictFloodFillToRevealedCells), false);
    }
}

public class FloodFillMod : Mod
{
    public static FloodFillSettings Settings { get; private set; } = new();

    public FloodFillMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FloodFillSettings>();
    }

    public override string SettingsCategory() => "Flood Fill Shape";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("Merthsoft_FloodFill_Settings_RestrictToRevealedCells".Translate(),
            ref Settings.RestrictFloodFillToRevealedCells,
            "Merthsoft_FloodFill_Settings_RestrictToRevealedCellsTooltip".Translate());
        listing.End();
    }
}
