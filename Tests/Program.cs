// Isolated algorithm fixtures; the reused designation list matches RimWorld 1.7.4977 IL.
// These checks do not simulate the game dragger or mod interoperability.
using Verse;
using Merthsoft.DesignatorShapes.FloodFill;
static class Program {
 static IntVec3 P(int x)=>new(x,0,0);
 static void Main(){
  var persisted=new FloodFillSettings();
 persisted.RestrictFloodFillToRevealedCells=true;persisted.ExposeData();
 Scribe_Values.Loading=true;var restored=new FloodFillSettings();restored.ExposeData();
 if(!restored.RestrictFloodFillToRevealedCells)throw new Exception("Setting did not round trip");
 Scribe_Values.Values.Clear();restored.ExposeData();if(restored.RestrictFloodFillToRevealedCells)throw new Exception("Missing setting did not default false");Scribe_Values.Loading=false;
 Console.WriteLine("PASS production settings expose true and missing-value false through Scribe fixture");
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
 foreach(bool rockBarrier in new[]{false,true}) {
 map=new Map();Find.CurrentMap=map;map.designationManager.cells.UnionWith(new[]{P(0),P(1),P(2)});
 map.thingGrid.cells[P(1)]=new(){rockBarrier?new Mineable{def=new ThingDef{defName="Rock"}}:new Thing{def=door}};
 if(!FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[]{P(0)}))throw new Exception("Plan crossed designated "+(rockBarrier?"rock":"wall"));
 Console.WriteLine("PASS plan cannot cross designated "+(rockBarrier?"rock":"wall")+" to outside plan");
 }
 map=new Map();Find.CurrentMap=map;map.thingGrid.cells[P(1)]=new(){new Thing{def=new ThingDef{entityDefToBuild=new TerrainDef()}}}; if(FloodFill.Fill(P(0)).Count()!=4)throw new Exception("Terrain blueprint"); Console.WriteLine("PASS terrain blueprint does not throw or block fill");
 map=new Map();Find.CurrentMap=map;map.width=2000;
 if(FloodFill.Fill(P(0)).Count()!=1500)throw new Exception("Cell limit");
 Console.WriteLine("PASS open fill retains 1500-cell cap");
 map=new Map();Find.CurrentMap=map;map.thingGrid.cells[P(1)]=new(){new Thing{def=door}};
 if(!FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[]{P(0)}))throw new Exception("Door");
 Console.WriteLine("PASS undesignated door blocks room fill");
 if(FloodFillMod.Settings.RestrictFloodFillToRevealedCells)throw new Exception("Restriction must default off");
 map=new Map();Find.CurrentMap=map;map.fogged.UnionWith(new[]{P(0),P(1)});
 if(FloodFill.Fill(P(0)).Count()!=4)throw new Exception("Default scouting changed");
 Console.WriteLine("PASS default permits fogged seed and traversal");
 FloodFillMod.Settings.RestrictFloodFillToRevealedCells=true;
 if(FloodFill.Fill(P(0)).Any())throw new Exception("Fogged seed accepted");
 Console.WriteLine("PASS enabled restriction rejects fogged seed");
 map=new Map();Find.CurrentMap=map;map.fogged.UnionWith(new[]{P(2),P(3)});
 if(!FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[]{P(0),P(1)}))throw new Exception("Fog boundary crossed");
 Console.WriteLine("PASS enabled restriction stops at fog boundary");
 map=new Map();Find.CurrentMap=map;map.fogged.Add(P(1));
 if(!FloodFill.Fill(P(0)).ToHashSet().SetEquals(new[]{P(0)}))throw new Exception("Hidden bridge connected revealed regions");
 Console.WriteLine("PASS hidden bridge cannot connect revealed regions");
 FloodFillMod.Settings.RestrictFloodFillToRevealedCells=false;
 if(FloodFill.Fill(P(0)).Count()!=4)throw new Exception("Disabling restriction did not restore scouting");
 Console.WriteLine("PASS disabling restores traversal through hidden bridge");
 var ore=new ThingDef{defName="Ore",fillPercent=1,building=new(){isNaturalRock=true,isResourceRock=true}};
 Map OreBlock(){var block=new Map{width=5,height=3};for(int x=0;x<5;x++)for(int z=0;z<3;z++)block.thingGrid.cells[new(x,0,z)]=new(){new Mineable{def=ore}};return block;}
 map=OreBlock();Find.CurrentMap=map;
 if(FloodFill.Fill(new(2,0,1)).Count()!=15)throw new Exception("Default concealed ore scouting changed");
 Console.WriteLine("PASS default preserves concealed unfogged ore scouting");
 FloodFillMod.Settings.RestrictFloodFillToRevealedCells=true;
 if(FloodFill.Fill(new(2,0,1)).Any())throw new Exception("Concealed unfogged ore seed accepted");
 Console.WriteLine("PASS enclosed unfogged ore seed rejected");
 for(int x=0;x<5;x++)map.thingGrid.cells.Remove(new(x,0,0));
 if(!FloodFill.Fill(new(2,0,1)).ToHashSet().SetEquals(Enumerable.Range(0,5).Select(x=>new IntVec3(x,0,1))))throw new Exception("Exposed vein leaked into interior or excluded visible face");
 Console.WriteLine("PASS exposed ore vein fills only exposed faces");
 map=OreBlock();Find.CurrentMap=map;map.thingGrid.cells.Remove(new(0,0,0));map.thingGrid.cells.Remove(new(4,0,0));
 if(!FloodFill.Fill(new(0,0,1)).ToHashSet().SetEquals(new[]{new IntVec3(0,0,1)}))throw new Exception("Concealed ore bridge crossed");
 Console.WriteLine("PASS concealed unfogged ore bridge cannot connect exposed ends");
 map=OreBlock();Find.CurrentMap=map;
 foreach(var neighbor in new[]{new IntVec3(1,0,1),new IntVec3(3,0,1),new IntVec3(2,0,0),new IntVec3(2,0,2)})map.thingGrid.cells[neighbor]=new(){new Thing{def=new ThingDef{defName="Wall",fillPercent=1,designationCategory=new(){defName="Structure"}}}};
 if(FloodFill.Fill(new(2,0,1)).Any())throw new Exception("Constructed walls falsely exposed ore");
 Console.WriteLine("PASS full constructed walls conceal ore without Rock links");
 map.thingGrid.cells[new(2,0,0)].Clear();
 if(!FloodFill.Fill(new(2,0,1)).ToHashSet().SetEquals(new[]{new IntVec3(2,0,1)}))throw new Exception("Opening constructed wall did not expose ore");
 Console.WriteLine("PASS opening constructed wall exposes ore face");
 map.linkGrid.rocks.Add(new(2,0,0));
 if(FloodFill.Fill(new(2,0,1)).Any())throw new Exception("Rock link treated as exposed face");
 Console.WriteLine("PASS Rock-linked neighbor conceals face even without full edifice");
 map=OreBlock();Find.CurrentMap=map;map.thingGrid.cells[new(2,0,1)]=new(){new Mineable{def=new ThingDef{defName="ModResource",fillPercent=1,building=new(){isResourceRock=true}}}};
 if(FloodFill.Fill(new(2,0,1)).Any())throw new Exception("Resource-only flagged mineable bypassed exposure");
 Console.WriteLine("PASS resource rock flag alone requires exposed face");
 FloodFillMod.Settings.RestrictFloodFillToRevealedCells=false;
 }
}
namespace RimWorld { }
namespace Verse {
 public record struct IntVec3(int x,int y,int z){public static IntVec3 North=>new(0,0,1);public static IntVec3 South=>new(0,0,-1);public static IntVec3 East=>new(1,0,0);public static IntVec3 West=>new(-1,0,0);public static IntVec3 operator +(IntVec3 a,IntVec3 b)=>new(a.x+b.x,0,a.z+b.z);public bool InBounds(Map m)=>x>=0&&x<m.width&&z>=0&&z<m.height;public Thing GetEdifice(Map m)=>m.thingGrid.ThingsListAtFast(this).FirstOrDefault();public bool Fogged(Map m)=>m.fogged.Contains(this);public List<Thing> GetThingList(Map m)=>m.thingGrid.ThingsListAtFast(this);}
 public class Def {public string defName="";}
 public class BuildableDef:Def {public Def designationCategory;}
 public class ThingDef:BuildableDef {public BuildableDef entityDefToBuild;public bool coversFloor;public BuildingProperties building;public float fillPercent;}
 public class TerrainDef:BuildableDef {public bool IsFloor;}
 public class BuildingProperties {public bool isNaturalRock;public bool isResourceRock;}
 public static class GenAdj {public static IntVec3[] CardinalDirections={IntVec3.North,IntVec3.East,IntVec3.South,IntVec3.West};}
 public class Thing {public ThingDef def;}
 public class Mineable:Thing {}
 public class Designation {}
 [Flags] public enum LinkFlags {None=0,Rock=1}
 public class LinkGrid {public HashSet<IntVec3> rocks=new();public LinkFlags LinkFlagsAt(IntVec3 c)=>rocks.Contains(c)?LinkFlags.Rock:LinkFlags.None;}
 public class Map {public LinkGrid linkGrid=new();public HashSet<IntVec3> fogged=new();public int height=1;public int width=4;public ThingGrid thingGrid=new();public DesignationManager designationManager=new();public TerrainGrid terrainGrid=new();public FertilityGrid fertilityGrid=new();}
 public class ThingGrid {public Dictionary<IntVec3,List<Thing>> cells=new();public List<Thing> ThingsListAtFast(IntVec3 c){if(!c.InBounds(Find.CurrentMap))throw new Exception("Off-map query");return cells.TryGetValue(c,out var t)?t:new();}}
 public class DesignationManager {public HashSet<IntVec3> cells=new();static List<Designation> tmp=new();public IEnumerable<Designation> AllDesignationsAt(IntVec3 c){tmp.Clear();if(cells.Contains(c))tmp.Add(new());return tmp;}}
 public class TerrainGrid {public TerrainDef TerrainAt(IntVec3 c)=>new();}
 public class FertilityGrid {public float FertilityAt(IntVec3 c)=>1;}
 public class Designator {public (bool Accepted,int unused) CanDesignateCell(IntVec3 c)=>(true,0);}
 public class DesignatorManager {public Designator SelectedDesignator=new();}
 public class PlaySettings {public bool showFertilityOverlay;}
 public static class Find {public static Map CurrentMap;public static DesignatorManager DesignatorManager=new();public static PlaySettings PlaySettings=new();}
}

namespace UnityEngine {public struct Rect {}}
namespace Verse {
 public class ModSettings {public virtual void ExposeData(){}}
 public class ModContentPack {}
 public class Mod {public Mod(ModContentPack p){}public T GetSettings<T>() where T:new()=>new();public virtual string SettingsCategory()=>"";public virtual void DoSettingsWindowContents(UnityEngine.Rect r){}}
 public class Listing_Standard {public void Begin(UnityEngine.Rect r){}public void End(){}public void CheckboxLabeled(string text,ref bool value,string tip){}}
 public static class Translation {public static string Translate(this string text)=>text;}
 public static class Scribe_Values {public static bool Loading;public static Dictionary<string,bool> Values=new();public static void Look(ref bool value,string key,bool defaultValue){if(Loading)value=Values.TryGetValue(key,out var saved)?saved:defaultValue;else Values[key]=value;}}
}