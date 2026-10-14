using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

public enum FillCellOwner { Native, Integrated, Unknown }

public static class NativeFillOwnership
{
    private static readonly Lazy<Type> hostType = new(() => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType("Merthsoft.DesignatorShapes.DesignatorShapes", false))
        .FirstOrDefault(t => t != null));

    public static FillCellOwner Current()
    {
        return Inspect(hostType.Value);
    }

    internal static FillCellOwner Inspect(Type host)
    {
        if (host == null) return FillCellOwner.Native;
        try
        {
            var show = host.GetProperty("ShowControls", BindingFlags.Public | BindingFlags.Static);
            var tool = host.GetProperty("CurrentTool", BindingFlags.Public | BindingFlags.Static);
            if (show?.PropertyType != typeof(bool) || show.GetMethod == null || tool?.GetMethod == null)
                throw new InvalidOperationException("Designator Shapes ownership API is unavailable.");
            // Matches the integrated UpdateDragCellsIfNeeded replacement predicate.
            return (bool)show.GetValue(null) && tool.GetValue(null) != null
                ? FillCellOwner.Integrated : FillCellOwner.Native;
        }
        catch (Exception)
        {
            Log.WarningOnce("Flood Fill Shape could not determine Designator Shapes cell ownership. Destructive native fills requiring confirmation will be cancelled until compatible host state is available.", 353442364);
            return FillCellOwner.Unknown;
        }
    }
}
