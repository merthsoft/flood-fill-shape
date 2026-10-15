using System;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

// Window.PreOpen deselects the tool for Dialog-layer windows while playing.
// Skip that layer-specific reset, keeping standard initialization and dialog ordering.
public sealed class FloodFillConfirmationDialog : Dialog_MessageBox
{
    public FloodFillConfirmationDialog(TaggedString text, Action confirm, Action cancel)
        : base(text,
            buttonAText: "Merthsoft_FloodFill_ConfirmApply".Translate(), buttonAAction: confirm,
            buttonBText: "Merthsoft_FloodFill_ConfirmKeep".Translate(), buttonBAction: cancel,
            buttonADestructive: true, cancelAction: cancel)
    {
    }

    public override void PreOpen()
    {
        var originalLayer = layer;
        try
        {
            layer = WindowLayer.GameUI;
            base.PreOpen();
        }
        finally
        {
            layer = originalLayer;
        }
    }
}
