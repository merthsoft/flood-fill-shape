// Isolated algorithm fixtures; the reused designation list matches RimWorld 1.7.4977 IL.
// These checks do not simulate the game dragger or mod interoperability.
using Verse;
using Merthsoft.DesignatorShapes.FloodFill;
static class Program {
 static IntVec3 P(int x)=>new(x,0,0);
 static void Main(){
 Find.CurrentMap=null; if(FloodFill.Fill(P(0)).Any())throw new Exception("No-map preview"); Console.WriteLine("PASS absent map returns empty");
 var map=new Map();Find.CurrentMap=map; if(FloodFill.Fill(P(-1)).Any())throw new Exception("Off-map preview"); Console.WriteLine("PASS off-map seed returns empty");
 var door=new ThingDef{defName="Door",designationCategory=new(){defName="Structure"}};
 map.thingGrid.cells[P(1)]=new(){new Thing{def=door}};
 map.designationManager.cells.Add(P(1));
 var fill=FloodFill.Fill(P(0)).ToHashSet();
 if(!fill.SetEquals(new[]{P(0)}))throw new Exception("Fill escaped through designated door: "+fill.Count);
 Console.WriteLine("PASS designated door blocks fill from undesignated cell");
 map=new Map();Find.CurrentMap=map;map.designationManager.cells.UnionWith(new[]{P(0),P(1)});
 fill=FloodFill.Fill(P(0)).ToHashSet();
 if(!fill.SetEquals(new[]{P(0),P(1)}))throw new Exception("Designation seed changed during traversal: "+fill.Count);
 Console.WriteLine("PASS designated seed stays restricted to designated cells");
 map=new Map();Find.CurrentMap=map;map.thingGrid.cells[P(1)]=new(){new Thing{def=new ThingDef{entityDefToBuild=new TerrainDef()}}}; if(FloodFill.Fill(P(0)).Count()!=4)throw new Exception("Terrain blueprint"); Console.WriteLine("PASS terrain blueprint does not throw or block fill");
 map=new Map();Find.CurrentMap=map;map.width=2000;
 if(FloodFill.Fill(P(0)).Count()!=1500)throw new Exception("Cell limit");
 Console.WriteLine("PASS open fill retains 1500-cell cap");
 map=new Map();Find.CurrentMap=map;map.thingGrid.cells[P(1)]=new(){new Thing{def=door}};
 if(!FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[]{P(0)}))throw new Exception("Door");
 Console.WriteLine("PASS undesignated door blocks room fill");
 }
}
namespace RimWorld { }
namespace Verse {
 public record struct IntVec3(int x,int y,int z){public static IntVec3 North=>new(0,0,1);public static IntVec3 South=>new(0,0,-1);public static IntVec3 East=>new(1,0,0);public static IntVec3 West=>new(-1,0,0);public static IntVec3 operator +(IntVec3 a,IntVec3 b)=>new(a.x+b.x,0,a.z+b.z);public bool InBounds(Map m)=>x>=0&&x<m.width&&z==0;public List<Thing> GetThingList(Map m)=>m.thingGrid.ThingsListAtFast(this);}
 public class Def {public string defName="";}
 public class BuildableDef:Def {public Def designationCategory;}
 public class ThingDef:BuildableDef {public BuildableDef entityDefToBuild;public bool coversFloor;}
 public class TerrainDef:BuildableDef {public bool IsFloor;}
 public class Thing {public ThingDef def;}
 public class Mineable:Thing {}
 public class Designation {}
 public class Map {public int width=4;public ThingGrid thingGrid=new();public DesignationManager designationManager=new();public TerrainGrid terrainGrid=new();public FertilityGrid fertilityGrid=new();}
 public class ThingGrid {public Dictionary<IntVec3,List<Thing>> cells=new();public List<Thing> ThingsListAtFast(IntVec3 c){if(!c.InBounds(Find.CurrentMap))throw new Exception("Off-map query");return cells.TryGetValue(c,out var t)?t:new();}}
 public class DesignationManager {public HashSet<IntVec3> cells=new();static List<Designation> tmp=new();public IEnumerable<Designation> AllDesignationsAt(IntVec3 c){tmp.Clear();if(cells.Contains(c))tmp.Add(new());return tmp;}}
 public class TerrainGrid {public TerrainDef TerrainAt(IntVec3 c)=>new();}
 public class FertilityGrid {public float FertilityAt(IntVec3 c)=>1;}
 public class Designator {public (bool Accepted,int unused) CanDesignateCell(IntVec3 c)=>(true,0);}
 public class DesignatorManager {public Designator SelectedDesignator=new();}
 public class PlaySettings {public bool showFertilityOverlay;}
 public static class Find {public static Map CurrentMap;public static DesignatorManager DesignatorManager=new();public static PlaySettings PlaySettings=new();}
}

