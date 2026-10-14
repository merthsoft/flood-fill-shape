using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Merthsoft.DesignatorShapes.FloodFill;

public static class FloodFill
{
    public const int FloodFillCellLimit = 1500;

    public static IEnumerable<IntVec3> Fill(IntVec3 t)
    {
        var ret = new HashSet<IntVec3>();
        var map = Find.CurrentMap;
        var restrictToRevealedCells = FloodFillMod.Settings.RestrictFloodFillToRevealedCells;
        if (map == null || !t.InBounds(map) || (restrictToRevealedCells && !IsRevealedForFill(t, map)))
            return ret;

        var wallAtMouse = map.getWallDefAt(t);
        // AllDesignationsAt returns a reused temporary list. Snapshot the seed before querying other cells.
        var hasDesignationsAtMouse = map.getDesignationsAt(t).Any();
        var mineableAtMouse = map.getMineableAt(t);
        var floorAtMouse = map.getTerrainAt(t);
        var fertAtMouse = map.getFertilityAt(t);

        var cells = new Queue<IntVec3>();
        cells.Enqueue(t);

        while (cells.Count() > 0 && ret.Count() < FloodFillCellLimit)
        {
            var cell = cells.Dequeue();
            if (!cell.InBounds(map) || (restrictToRevealedCells && !IsRevealedForFill(cell, map)))
                continue;
            if (ret.Contains(cell))
                continue;
            if (!Find.DesignatorManager.SelectedDesignator.CanDesignateCell(cell).Accepted)
                continue;
            var cellWall = map.getWallDefAt(cell);
            var cellDes = map.getDesignationsAt(cell);
            var cellMineable = map.getMineableAt(cell);
            var cellFloor = map.getTerrainAt(cell);
            var cellFert = map.getFertilityAt(cell);
            var cellThings = map.thingGrid.ThingsListAtFast(cell);

            var addFlag = false;

            if (wallAtMouse != null)
            {
                if (cellWall == null)
                    continue;
                if (cellWall.defName == wallAtMouse.defName)
                    addFlag = true;
            }
            else if (mineableAtMouse != null)
            {
                if (cellMineable?.def == mineableAtMouse.def)
                    addFlag = true;
            }
            else if (hasDesignationsAtMouse)
            {
                if (cellWall == null && cellMineable == null && cellDes.Any())
                    addFlag = true;
            }
            else if (cellWall == null && cellMineable == null)
            {
                if (Find.PlaySettings.showFertilityOverlay && fertAtMouse != cellFert && !floorAtMouse.IsFloor && !cellFloor.IsFloor)
                    addFlag = false;
                else
                {
                    addFlag = true;
                    foreach (var thing in cellThings)
                    {
                        var def = thing.def.entityDefToBuild == null ? thing.def : thing.def.entityDefToBuild as ThingDef;
                        if (def?.coversFloor == true || def.IsStructure())
                        {
                            addFlag = false;
                            break;
                        }
                    }
                }
            }

            if (addFlag)
            {
                ret.Add(cell);
                EnqueueRevealedNeighbor(cell + IntVec3.North);
                EnqueueRevealedNeighbor(cell + IntVec3.East);
                EnqueueRevealedNeighbor(cell + IntVec3.South);
                EnqueueRevealedNeighbor(cell + IntVec3.West);
            }
        }

        return ret;

        void EnqueueRevealedNeighbor(IntVec3 neighbor)
        {
            if (!restrictToRevealedCells || (neighbor.InBounds(map) && IsRevealedForFill(neighbor, map)))
                cells.Enqueue(neighbor);
        }
    }

    // Vanilla has no per-ore discovery flag. Require an exposed face for natural rock,
    // rather than allowing unfogged, enclosed interiors to disclose their ore type.
    private static bool IsRevealedForFill(IntVec3 cell, Map map)
    {
        if (cell.Fogged(map))
            return false;
        var mineable = map.getMineableAt(cell);
        if (mineable?.def.building?.isNaturalRock != true && mineable?.def.building?.isResourceRock != true)
            return true;

        foreach (var direction in GenAdj.CardinalDirections)
        {
            var neighbor = cell + direction;
            if (neighbor.InBounds(map) && !neighbor.Fogged(map)
                && (map.linkGrid.LinkFlagsAt(neighbor) & LinkFlags.Rock) == 0
                && (neighbor.GetEdifice(map)?.def.fillPercent ?? 0f) < 1f)
                return true;
        }
        return false;
    }
    private static float getFertilityAt(this Map map, IntVec3 cell)
        => map.fertilityGrid.FertilityAt(cell);

    private static TerrainDef getTerrainAt(this Map map, IntVec3 cell)
        => map.terrainGrid.TerrainAt(cell);

    private static IEnumerable<Designation> getDesignationsAt(this Map map, IntVec3 cell)
        => map.designationManager.AllDesignationsAt(cell);

    private static Thing getMineableAt(this Map map, IntVec3 cell)
        => map.thingGrid.ThingsListAtFast(cell).FirstOrDefault(t => t is Mineable);

    private static bool IsStructure(this Def d)
        => (d as BuildableDef)?.designationCategory?.defName == "Structure";

    private static ThingDef GetStructureDef(this Thing thing)
        => thing.def.entityDefToBuild.IsStructure()
            ? thing.def.entityDefToBuild as ThingDef
            : thing.def.IsStructure()
                ? thing.def : null;

    private static Def getWallDefAt(this Map map, IntVec3 cell)
        => cell.GetThingList(map)
                .Select(t => t.GetStructureDef())
                .FirstOrDefault(t => t != null);
}