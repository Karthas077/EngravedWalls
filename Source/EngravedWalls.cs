using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace EW
{

    [StaticConstructorOnStartup]
    internal static class EW_Initializer
    {
        static EW_Initializer()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("net.karthas.rimworld.mod.engravestone");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [DefOf]
    public static class DesignationDefOf
    {
        public static DesignationDef SmoothWall;
        public static DesignationDef EngraveWall;
    }

    [DefOf]
    public static class JobDefOf
    {
        public static JobDef SmoothWall;
        public static JobDef EngraveWall;
    }

    [DefOf]
    public static class ThingDefOf
    {
        public static ThingDef SmoothedWall;
        public static ThingDef EngravedWall;
    }

    public class Designator_SmoothWall : Designator
    {
        public override int DraggableDimensions
        {
            get { return 2; }
        }
        public override bool DragDrawMeasurements
        {
            get { return true; }
        }
        public Designator_SmoothWall()
        {
            this.defaultLabel = "EW.DesignatorSmoothWall".Translate();
            this.icon = ContentFinder<Texture2D>.Get("SmoothWall", true);
            this.defaultDesc = "EW.DesignatorSmoothWallDesc".Translate();
            this.useMouseIcon = true;
            this.soundDragSustain = SoundDefOf.DesignateDragStandard;
            this.soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            this.soundSucceeded = SoundDefOf.DesignateMine;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
            {
                return false;
            }
            if (c.Fogged(base.Map))
            {
                return false;
            }
            if (!ResearchProjectDef.Named("Stonecutting").IsFinished)
            {
                return "EW.MustResearchStonecutting".Translate();
            }
            if (base.Map.designationManager.DesignationAt(c, DesignationDefOf.SmoothWall) != null)
            {
                return "EW.TerrainBeingSmoothed".Translate();
            }
            if (c.InNoBuildEdgeArea(base.Map))
            {
                return "TooCloseToMapEdge".Translate();
            }
            Thing thing = c.GetFirstMineable(base.Map);
            if (thing == null)
            {
                return "EW.MessageMustDesignateRough".Translate();
            }
            AcceptanceReport result = this.CanDesignateThing(thing);
            if (!result.Accepted)
            {
                return result;
            }
            List<Thing> list = base.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].def.building.isNaturalRock || list[i].def.defName.Equals("EngravedWall"))
                {
                    return AcceptanceReport.WasAccepted;
                }
            }
            return "EW.MessageMustDesignateRough".Translate();
        }
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (!t.def.mineable)
            {
                return false;
            }
            if (t.def.defName.Equals("SmoothedWall"))
            {
                return false;
            }
            if (base.Map.designationManager.DesignationAt(t.Position, DesignationDefOf.SmoothWall) != null)
            {
                return AcceptanceReport.WasRejected;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            base.Map.designationManager.AddDesignation(new Designation(loc, DesignationDefOf.SmoothWall));
        }
        public override void DesignateThing(Thing t)
        {
            this.DesignateSingleCell(t.Position);
        }
        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
    }

    public class Designator_EngraveWall : Designator
    {
        public override int DraggableDimensions
        {
            get { return 2; }
        }
        public override bool DragDrawMeasurements
        {
            get { return true; }
        }
        public Designator_EngraveWall()
        {
            this.defaultLabel = "EW.DesignatorEngraveWall".Translate();
            this.icon = ContentFinder<Texture2D>.Get("EngraveWall", true);
            this.defaultDesc = "EW.DesignatorEngraveWallDesc".Translate();
            this.useMouseIcon = true;
            this.soundDragSustain = SoundDefOf.DesignateDragStandard;
            this.soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            this.soundSucceeded = SoundDefOf.DesignateMine;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
            {
                return false;
            }
            if (c.Fogged(base.Map))
            {
                return false;
            }
            if (base.Map.designationManager.DesignationAt(c, DesignationDefOf.EngraveWall) != null)
            {
                return "EW.TerrainBeingEngraved".Translate();
            }
            if (c.InNoBuildEdgeArea(base.Map))
            {
                return "TooCloseToMapEdge".Translate();
            }
            List<Thing> list = base.Map.thingGrid.ThingsListAt(c);
            if (list.NullOrEmpty())
            {
                return "EW.MessageMustDesignateSmooth".Translate();
            }
            Thing thing = list[0];
            AcceptanceReport result = this.CanDesignateThing(thing);
            if (!result.Accepted)
            {
                return result;
            }
            if (thing.def.defName.Equals("SmoothedWall"))
            {
                return AcceptanceReport.WasAccepted;
            }
            if (list.Count > 1)
            {
                thing = list[1];
                result = this.CanDesignateThing(thing);
                if (!result.Accepted)
                {
                    return result;
                }
                if (thing.def.defName.Equals("SmoothedWall"))
                {
                    return AcceptanceReport.WasAccepted;
                }
            }
            return "EW.MessageMustDesignateSmooth".Translate();
        }
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (base.Map.designationManager.DesignationAt(t.Position, DesignationDefOf.EngraveWall) != null)
            {
                return AcceptanceReport.WasRejected;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            base.Map.designationManager.AddDesignation(new Designation(loc, DesignationDefOf.EngraveWall));
        }
        public override void DesignateThing(Thing t)
        {
            this.DesignateSingleCell(t.Position);
        }
        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
    }

    public abstract class JobDriver_SmoothWallBase : JobDriver
    {
        private float workLeft = -1000f;
        protected SkillDef skillGained = SkillDefOf.Construction;
        protected float skillGainSpeed = 0.055f;
        protected bool clearSnow;
        protected abstract float BaseWorkAmount { get; }
        protected abstract ThingDef WallStuff { get; }
        protected abstract DesignationDef DesDef { get; }
        protected virtual StatDef SpeedStat
        {
            get { return null; }
        }
        public override bool TryMakePreToilReservations()
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo targetA = this.job.targetA;
            Job job = this.job;
            ReservationLayerDef floor = ReservationLayerDefOf.Floor;
            return pawn.Reserve(targetA, job, 1, -1, floor);
        }
        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !this.job.ignoreDesignations && this.Map.designationManager.DesignationAt(this.TargetLocA, this.DesDef) == null);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            Toil doWork = new Toil();
            doWork.initAction = delegate {
                this.workLeft = this.BaseWorkAmount;
            };
            doWork.tickAction = delegate {
                float num = (this.SpeedStat == null) ? 1f : doWork.actor.GetStatValue(this.SpeedStat, true);
                this.workLeft -= num;
                if (doWork.actor.skills != null)
                {
                    doWork.actor.skills.Learn(skillGained, skillGainSpeed, false);
                }
                if (this.clearSnow)
                {
                    this.Map.snowGrid.SetDepth(this.TargetLocA, 0f);
                }
                if (this.workLeft <= 0f)
                {
                    this.DoEffect(this.TargetLocA);
                    Designation designation = this.Map.designationManager.DesignationAt(this.TargetLocA, this.DesDef);
                    if (designation != null)
                    {
                        designation.Delete();
                    }
                    this.ReadyForNextToil();
                    return;
                }
            };
            doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            doWork.WithProgressBar(TargetIndex.A, () => 1f - this.workLeft / this.BaseWorkAmount, false, -0.5f);
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            yield return doWork;
        }
        protected abstract void DoEffect(IntVec3 c);
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f, false);
        }
    }

    public class JobDriver_SmoothWall : JobDriver_SmoothWallBase
    {
        protected override float BaseWorkAmount
        {
            get { return ThingDefOf.SmoothedWall.GetStatValueAbstract(StatDefOf.WorkToBuild, this.WallStuff); }
        }
        protected override ThingDef WallStuff
        {
            get
            {
                List<Thing> list = base.Map.thingGrid.ThingsListAt(this.TargetLocA);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def.building.isNaturalRock)
                    {
                        return ThingDef.Named("Blocks" + list[i].def.defName);
                    }
                }
                return null;
            }
        }
        protected override DesignationDef DesDef
        {
            get { return DesignationDefOf.SmoothWall; }
        }
        protected override StatDef SpeedStat
        {
            get { return StatDefOf.SmoothingSpeed; }
        }
        public JobDriver_SmoothWall()
        {
            this.clearSnow = true;
        }
        protected override void DoEffect(IntVec3 c)
        {
            
            if (this.WallStuff != null)
            {
                Thing wall = ThingMaker.MakeThing(ThingDefOf.SmoothedWall, this.WallStuff);
                wall.SetFaction(Faction.OfPlayer, null);
                TempSupports.CreateInvisiColumns(c, base.Map);
                base.Map.thingGrid.ThingsListAt(this.TargetLocA)[0].DeSpawn();
                GenSpawn.Spawn(wall, c, base.Map);
                TempSupports.DeleteInvisiColumns(c, base.Map);
            }
        }
    }

    public class JobDriver_EngraveWall : JobDriver_SmoothWallBase
    {
        protected override float BaseWorkAmount
        {
            get { return ThingDefOf.EngravedWall.GetStatValueAbstract(StatDefOf.WorkToBuild, this.WallStuff); }
        }
        protected override ThingDef WallStuff
        {
            get
            {
                List<Thing> list = base.Map.thingGrid.ThingsListAt(this.TargetLocA);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def.defName.Equals("SmoothedWall"))
                    {
                        return list[i].Stuff;
                    }
                }
                return null;
            }
        }
        protected override DesignationDef DesDef
        {
            get { return DesignationDefOf.EngraveWall; }
        }
        protected override StatDef SpeedStat
        {
            get { return StatDefOf.WorkSpeedGlobal; }
        }
        public JobDriver_EngraveWall()
        {
            this.clearSnow = true;
            this.skillGained = SkillDefOf.Artistic;
            this.skillGainSpeed = 0.11f;
        }
        protected override void DoEffect(IntVec3 c)
        {
            if (this.WallStuff != null)
            {
                Thing wall = ThingMaker.MakeThing(ThingDefOf.EngravedWall, this.WallStuff);
                CompQuality compQuality = wall.TryGetComp<CompQuality>();
                if (compQuality != null)
                {
                    int level = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
                    QualityCategory qualityCategory = QualityUtility.RandomCreationQuality(level);
                    compQuality.SetQuality(qualityCategory, ArtGenerationContext.Colony);
                }
                CompArt compArt = wall.TryGetComp<CompArt>();
                if (compArt != null)
                {
                    compArt.JustCreatedBy(pawn);
                    if (compQuality.Quality >= QualityCategory.Excellent)
                    {
                        TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[]
                        {
                            pawn,
                            wall
                        });
                    }
                }
                wall.SetFaction(Faction.OfPlayer, null);
                TempSupports.CreateInvisiColumns(c, base.Map);
                List<Thing> list = base.Map.thingGrid.ThingsListAt(this.TargetLocA);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def.defName.Equals("SmoothedWall"))
                    {
                       list[i].DeSpawn();
                        break;
                    }
                }
                GenSpawn.Spawn(wall, c, base.Map);
                TempSupports.DeleteInvisiColumns(c, base.Map);
            }
        }
    }

    public class WorkGiver_SmoothWall : WorkGiver_ConstructAffectFloor
    {
        protected override DesignationDef DesDef
        {
            get { return DesignationDefOf.SmoothWall; }
        }
        public override Job JobOnCell(Pawn pawn, IntVec3 c)
        {
            return new Job(JobDefOf.SmoothWall, c);
        }
    }

    public class WorkGiver_EngraveWall : WorkGiver_ConstructAffectFloor
    {
        protected override DesignationDef DesDef
        {
            get { return DesignationDefOf.EngraveWall; }
        }
        public override Job JobOnCell(Pawn pawn, IntVec3 c)
        {
            return new Job(JobDefOf.EngraveWall, c);
        }
    }

    public static class TempSupports
    {
        public static void CreateInvisiColumns(IntVec3 wallLoc, Map map)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int x = wallLoc.x + i;
                    int z = wallLoc.z + j;
                    IntVec3 newSpot = new IntVec3(x, 0, z);
                    List<Thing> supportlist = map.thingGrid.ThingsListAt(newSpot);
                    if (supportlist.NullOrEmpty())
                    {
                        Thing support = ThingMaker.MakeThing(ThingDef.Named("InvisiColumn"));
                        GenSpawn.Spawn(support, newSpot, map);
                    }
                }
            }
        }
        public static void DeleteInvisiColumns(IntVec3 wallLoc, Map map)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int x = wallLoc.x + i;
                    int z = wallLoc.z + j;
                    IntVec3 newSpot = new IntVec3(x, 0, z);
                    List<Thing> supportlist = map.thingGrid.ThingsListAt(newSpot);
                    if (supportlist[0].def.defName == "InvisiColumn")
                    {
                        supportlist[0].DeSpawn();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GenLeaving), "DoLeavingsFor", new Type[] { typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(CellRect) })]
    public static class GenLeaving_DoLeavingsFor
    {
        public static bool Prefix(Thing diedThing, Map map, DestroyMode mode, CellRect leavingsRect)
        {
            if (diedThing.def.defName.Equals("SmoothedWall") || diedThing.def.defName.Contains("EngravedWall"))
            {
                if (Rand.Value < 0.75f) { return false; }
                List<ThingCountClass> thingCountClasses = diedThing.CostListAdjusted();
                for (int l = 0; l < thingCountClasses.Count; l++)
                {
                    ThingCountClass thingCountClass = thingCountClasses[l];
                    List<IntVec3> listEW = GenCollection.InRandomOrder<IntVec3>(leavingsRect.Cells, null).ToList<IntVec3>();
                    if (thingCountClass.thingDef.defName.Contains("Blocks"))
                    {
                        string chunkType = "Chunk" + thingCountClass.thingDef.defName.Substring(6);
                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDef.Named(chunkType), null), listEW[0], map, ThingPlaceMode.Near, null);
                    }
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TouchPathEndModeUtility), "IsCornerTouchAllowed", null)]
    public static class TouchPathEndModeUtility_IsCornerTouchAllowed
    {
        public static bool Prefix(int cornerX, int cornerZ, Map map, ref bool __result)
        {
            IntVec3 cell = new IntVec3(cornerX, 0, cornerZ);
            if (map.designationManager.DesignationAt(cell, DesignationDefOf.SmoothWall) != null)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver", null)]
    public static class GenConstruct_CanPlaceBlueprintOver
    {
        public static bool Prefix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
        {
            ThingDef thingDef = newDef as ThingDef;
            ThingDef thingDef1 = oldDef;
            if (thingDef == null || thingDef1 == null)
            {
                return true;
            }
            BuildableDef buildableDef = GenConstruct.BuiltDefOf(oldDef);
            ThingDef thingDef2 = buildableDef as ThingDef;
            if (thingDef.building != null && thingDef.building.canPlaceOverWall && thingDef2 != null)
            {
                if (thingDef2.defName.Equals("SmoothedWall") || (thingDef2.defName.Contains("EngravedWall")))
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenConstruct), "BlocksConstruction", null)]
    public static class GenConstruct_BlocksConstruction
    {
        public static bool Prefix(Thing constructible, Thing t, ref bool __result)
        {
            ThingDef thingDef;
            if (constructible == null || t == null)
            {
                return true;
            }
            if (!(constructible is Blueprint))
            {
                thingDef = (!(constructible is Frame) ? constructible.def.blueprintDef : constructible.def.entityDefToBuild.blueprintDef);
            }
            else
            {
                thingDef = constructible.def;
            }
            ThingDef thingDef1 = thingDef.entityDefToBuild as ThingDef;
            if (thingDef1 != null && thingDef1.building != null && thingDef1.building.canPlaceOverWall)
            {
                if (t.def.defName.Equals("SmoothedWall") || (t.def.defName.Contains("EngravedWall")))
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenSpawn), "SpawningWipes", null)]
    public static class GenSpawn_SpawningWipes
    {
        public static bool Prefix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
        {
            ThingDef thingDef = newEntDef as ThingDef;
            ThingDef thingDef1 = oldEntDef as ThingDef;
            if (thingDef == null || thingDef1 == null)
            {
                return true;
            }
            ThingDef thingDef2 = thingDef.entityDefToBuild as ThingDef;
            if (thingDef1.IsBlueprint)
            {
                if (thingDef.IsBlueprint)
                {
                    if (thingDef2 != null && thingDef2.building != null && thingDef2.building.canPlaceOverWall)
                    {
                        if (thingDef1.entityDefToBuild is ThingDef)
                        {
                            if (thingDef1.entityDefToBuild.defName.Equals("SmoothedWall") || thingDef1.entityDefToBuild.defName.Contains("EngravedWall"))
                            {
                                __result = false;
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ItemCollectionGenerator_Meteorite), "Reset", null)]
    public static class ItemCollectionGenerator_Meteorite_Reset
    {
        public static bool Prefix()
        {
            ItemCollectionGenerator_Meteorite.mineables.Clear();
            ItemCollectionGenerator_Meteorite.mineables.AddRange(from x in DefDatabase<ThingDef>.AllDefsListForReading
                                                                 where x.mineable && x != RimWorld.ThingDefOf.CollapsedRocks && !x.defName.Contains("Engraved")
                                                                 select x);
            return false;
        }
    }

}
