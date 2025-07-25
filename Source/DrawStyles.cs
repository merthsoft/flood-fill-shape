using System.Collections.Generic;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

public class Fill : Verse.DrawStyle
{
    public override void Update(IntVec3 origin, IntVec3 target, List<IntVec3> buffer)
    {
        buffer.Clear();
        buffer.AddRange(FloodFill.Fill(target));
    }
}