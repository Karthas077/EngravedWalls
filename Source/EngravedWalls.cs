using Harmony;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace EW
{
    [DefOf]
    public static class DesignationDefOf
    {
        public static DesignationDef EngraveWall;
    }

    [DefOf]
    public static class JobDefOf
    {
        public static JobDef EngraveWall;
    }

    [DefOf]
    public static class RulePackDefOf
    {
        public static RulePackDef ArtDescription_EngravedWall;
    }

    public class EngravedWalls : Mod
    {
        public static ModContentPack thisPack;
        public EngravedWalls(ModContentPack content) : base(content)
        {
            thisPack = content;
            HarmonyInstance harmony = HarmonyInstance.Create("net.karthas.rimworld.mod.engravestone1");
            //Postfix
            harmony.Patch(AccessTools.Property(typeof(ThingDef), "IsSmoothable").GetGetMethod(), null,
                new HarmonyMethod(typeof(EngravedWalls), nameof(IsSmoothable)), null);
            //harmony.Patch(AccessTools.Method(typeof(ThingDef), "IsSmoothable"), null,
            //    new HarmonyMethod(typeof(EngravedWalls), nameof(IsSmoothable)), null);
            //Prefix
            harmony.Patch(AccessTools.Method(typeof(SmoothableWallUtility), "SmoothWall"),
                new HarmonyMethod(typeof(EngravedWalls), nameof(SmoothWall)), null, null);
            //Prefix
            harmony.Patch(AccessTools.Method(typeof(Designator_Mine), "DesignateSingleCell"),
                new HarmonyMethod(typeof(EngravedWalls), nameof(DesignateSingleCell)), null, null);
            //Postfix
            harmony.Patch(AccessTools.Method(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve"), null,
                new HarmonyMethod(typeof(EngravedWalls), nameof(GenerateImpliedDefs_PreResolve)), null);


        }

        //Patch to allow Engraved walls to be designated to be smoothed.
        public static void IsSmoothable(ThingDef __instance, ref bool __result)
        {
            if(__instance.defName !=null && __instance.defName.Contains("Engraved"))
            {
                __result = true;
            }
        }

        //Patch to actually smooth an engraved wall
        //Could be replaced with a Transpiler but that's complicated and unneeded unless other people start patching this
        public static bool SmoothWall(Thing target, Pawn smoother, ref Thing __result)
        {
            if(target.def.defName.Contains("Engraved"))
            {
                Map map = target.Map;
                target.Destroy(DestroyMode.WillReplace);
                //Substring to find the defName of the natural rock, ThingDef from defName, get the ThingDef it smooths into
                Thing thing = ThingMaker.MakeThing(ThingDef.Named(target.def.defName.Substring(8)).building.smoothedThing, target.Stuff);
                thing.SetFaction(smoother.Faction, null);
                GenSpawn.Spawn(thing, target.Position, map, target.Rotation, WipeMode.Vanish, false);
                map.designationManager.TryRemoveDesignation(target.Position, RimWorld.DesignationDefOf.SmoothWall);
                __result = thing;
                return false;
            }
            return true;
        }

        //Patch to cancel engraving on mining designation
        public static void DesignateSingleCell(Designator_Mine __instance, IntVec3 loc)
        {
            __instance.Map.designationManager.TryRemoveDesignation(loc, DesignationDefOf.EngraveWall);
        }

        //Patch to auto-generate EngravedWall ThingDefs for all added natural rocks
        public static void GenerateImpliedDefs_PreResolve()
        {
            IEnumerable<ThingDef> enumerable = ImpliedThingDefs();
            foreach (ThingDef def in enumerable)
            {
                DefGenerator.AddImpliedDef<ThingDef>(def);
            }
        }

        //Enumerator to process the existing ThingDefs
        public static IEnumerable<ThingDef> ImpliedThingDefs()
        {
            int i = 0;
            foreach (ThingDef rock in from def in DefDatabase<ThingDef>.AllDefs.ToList<ThingDef>()
                                      where def.building != null && def.building.isNaturalRock && !def.building.isResourceRock && !def.defName.Contains("Engraved")
                                      select def)
            {
                //Building a new ThingDef from the root up!
                ThingDef engraved = new ThingDef();
                //BuildingNaturalBase
                engraved.category = ThingCategory.Building;
                engraved.selectable = true;
                engraved.drawerType = DrawerType.MapMeshOnly;
                engraved.filthLeaving = RimWorld.ThingDefOf.Filth_RubbleRock;
                engraved.scatterableOnMapGen = false;
                //RockBase
                engraved.thingClass = typeof(Mineable);
                //engraved.graphicData.texPath = "WallsEngraved"; //Overwritten
                engraved.graphicData = new GraphicData();
                engraved.graphicData.graphicClass = typeof(Graphic_Random);
                engraved.graphicData.linkType = LinkDrawerType.CornerFiller;
                engraved.graphicData.linkFlags = LinkFlags.MapEdge | LinkFlags.Rock;
                engraved.altitudeLayer = AltitudeLayer.Building;
                engraved.passability = Traversability.Impassable;
                engraved.blockWind = true;
                engraved.castEdgeShadows = true;
                engraved.fillPercent = 1;
                engraved.coversFloor = true;
                engraved.neverMultiSelect = true;
                engraved.rotatable = false;
                //engraved.saveCompressible = true; //Overwritten
                engraved.holdsRoof = true;
                engraved.staticSunShadowHeight = 1.0F;
                engraved.blockLight = true;
                engraved.mineable = true;
                //StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.Flammability, 0F); //Overwritten
                engraved.building = new BuildingProperties();
                engraved.building.isInert = true;
                //engraved.building.isNaturalRock = true; //Overwritten
                //engraved.building.canBuildNonEdificesUnder = false; //Overwritten
                //engraved.building.deconstructible = false; //Private variable...
                //UglyRockBase
                StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.Flammability, 0F);
                //StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.Beauty, -2F); //Overwritten
                engraved.building.mineableYieldWasteable = false;
                //StoneBase
                //engraved.defName = "Engraved" + rock.defName; //Overwritten
                //engraved.label = "engraved " + rock.label; //Overwritten
                //engraved.description = "EngravedStoneDesc".Translate(); //Overwritten
                engraved.graphicData.color = rock.graphicData.color;
                StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.MaxHitPoints, rock.GetStatValueAbstract(StatDefOf.MaxHitPoints));
                engraved.building.mineableThing = rock.building.mineableThing;
                engraved.building.mineableDropChance = rock.building.mineableDropChance;
                //engraved.building.smoothedThing = rock.building.smoothedThing; //Can't have multiple things smooth to the same thing.
                //SmoothedStoneBase
                engraved.defName = "Engraved" + rock.defName;
                engraved.label = "EngravedStoneLabel".Translate(new object[]
                {
                    rock.label
                });
                engraved.description = "EngravedStoneDesc".Translate(new object[]
                {
                    rock.label
                });
                engraved.graphicData.texPath = "WallsEngraved";
                StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.Beauty, 1F);
                StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.MarketValue, 18F);
                engraved.building.isNaturalRock = false; //Unfortunately this needs to remain true to prevent smoothed walls from defaulting to deconstructible.
                engraved.building.canBuildNonEdificesUnder = true;
                engraved.saveCompressible = false;
                //Art
                StatUtility.SetStatValueInList(ref engraved.statBases, StatDefOf.WorkToBuild, 2260F);
                engraved.comps = new List<CompProperties>();
                engraved.comps.Add(new CompProperties(typeof(CompQuality)));
                engraved.comps.Add(new CompProperties_Art
                {
                    nameMaker = RimWorld.RulePackDefOf.NamerArtSculpture,
                    descriptionMaker = RulePackDefOf.ArtDescription_EngravedWall,
                    canBeEnjoyedAsArt = true
                });
                engraved.inspectorTabs = new List<Type>();
                engraved.inspectorTabs.Add(typeof(ITab_Art));
                engraved.modContentPack = thisPack;
                Log.Message($"Created {engraved.defName} from {rock.defName}");
                yield return engraved;
                i++;
            }
            yield break;
        }
    }

    public class Designator_EngraveWall : Designator_SmoothSurface
    {
        public Designator_EngraveWall()
        {
            this.defaultLabel = "DesignatorEngraveWall".Translate();
            this.defaultDesc = "DesignatorEngraveWallDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("EngraveWall", true);
            this.useMouseIcon = true;
            this.soundDragSustain = SoundDefOf.Designate_DragStandard;
            this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            this.soundSucceeded = SoundDefOf.Designate_SmoothSurface;
            this.hotKey = KeyBindingDefOf.Misc1;
        }
        
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            AcceptanceReport result;
            if (!c.InBounds(base.Map))
            {
                result = false;
            }
            else if (c.Fogged(base.Map))
            {
                result = false;
            }
            else if (base.Map.designationManager.DesignationAt(c, DesignationDefOf.EngraveWall) != null)
            {
                result = "SurfaceBeingEngraved".Translate();
            }
            else if (c.InNoBuildEdgeArea(base.Map))
            {
                result = "TooCloseToMapEdge".Translate();
            }
            else
            {
                Building edifice = c.GetEdifice(base.Map);
                if (edifice != null && edifice.def.IsSmoothed)
                {
                    result = AcceptanceReport.WasAccepted;
                }
                else
                {
                    result = "MessageMustDesignateSmooth".Translate();
                }
            }
            return result;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Building edifice = c.GetEdifice(base.Map);
            if (edifice != null && edifice.def.IsSmoothed)
            {
                base.Map.designationManager.AddDesignation(new Designation(c, DesignationDefOf.EngraveWall));
                base.Map.designationManager.TryRemoveDesignation(c, RimWorld.DesignationDefOf.Mine);
            }
        }
    }
    
    public class JobDriver_EngraveWall : JobDriver
    {
        private float workLeft = -1000f;

        public JobDriver_EngraveWall()
        {
        }

        protected float BaseWorkAmount
        {
            get
            {
                return ThingDef.Named("Engraved" + base.TargetA.Thing.def.building.unsmoothedThing.defName).GetStatValueAbstract(StatDefOf.WorkToBuild, base.TargetA.Thing.Stuff);
            }
        }

        protected DesignationDef DesDef
        {
            get
            {
                return DesignationDefOf.EngraveWall;
            }
        }

        protected StatDef SpeedStat
        {
            get
            {
                return StatDefOf.WorkSpeedGlobal;
            }
        }

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null) && this.pawn.Reserve(this.job.targetA.Cell, this.job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !this.job.ignoreDesignations && this.Map.designationManager.DesignationAt(this.TargetLocA, this.DesDef) == null);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            Toil doWork = new Toil();
            doWork.initAction = delegate()
            {
                this.workLeft = this.BaseWorkAmount;
            };
            doWork.tickAction = delegate()
            {
                float num = (this.SpeedStat == null) ? 1f : doWork.actor.GetStatValue(this.SpeedStat, true);
                this.workLeft -= num;
                if (doWork.actor.skills != null)
                {
                    doWork.actor.skills.Learn(SkillDefOf.Artistic, 0.11f, false);
                }
                if (this.workLeft <= 0f)
                {
                    this.DoEffect();
                    Designation designation = this.Map.designationManager.DesignationAt(this.TargetLocA, this.DesDef);
                    if (designation != null)
                    {
                        designation.Delete();
                    }
                    this.ReadyForNextToil();
                }
            };
            doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            doWork.WithProgressBar(TargetIndex.A, () => 1f - this.workLeft / this.BaseWorkAmount, false, -0.5f);
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            doWork.activeSkill = (() => SkillDefOf.Artistic);
            yield return doWork;
            yield break;
        }

        protected void DoEffect()
        {
            Thing target = base.TargetA.Thing;
            Map map = target.Map;
            target.Destroy(DestroyMode.WillReplace);
            Thing wall = ThingMaker.MakeThing(ThingDef.Named("Engraved" + target.def.building.unsmoothedThing.defName), target.Stuff);
            CompQuality compQuality = wall.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                int level = this.pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
                QualityCategory qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(level, false);
                compQuality.SetQuality(qualityCategory, ArtGenerationContext.Colony);
            }
            CompArt compArt = wall.TryGetComp<CompArt>();
            if (compArt != null)
            {
                compArt.JustCreatedBy(this.pawn);
                if (compQuality.Quality >= QualityCategory.Excellent)
                {
                    TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[]
                    {
                        this.pawn,
                        wall
                    });
                }
            }
            wall.SetFaction(this.pawn.Faction, null);
            GenSpawn.Spawn(wall, target.Position, map, target.Rotation, WipeMode.Vanish, false);
            map.designationManager.TryRemoveDesignation(target.Position, DesignationDefOf.EngraveWall);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", 0f, false);
        }
    }

    public class WorkGiver_EngraveWall : WorkGiver_ConstructSmoothWall
    {
        public WorkGiver_EngraveWall()
        {
        }

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            foreach (Designation des in pawn.Map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.EngraveWall))
            {
                yield return des.target.Cell;
            }
            yield break;
        }

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            bool result;
            if (c.IsForbidden(pawn) || pawn.Map.designationManager.DesignationAt(c, DesignationDefOf.EngraveWall) == null)
            {
                result = false;
            }
            else
            {
                Building edifice = c.GetEdifice(pawn.Map);
                if (edifice == null || !edifice.def.IsSmoothed)
                {
                    Log.ErrorOnce("Failed to find valid edifice when trying to engrave a wall", 58988176, false);
                    pawn.Map.designationManager.TryRemoveDesignation(c, DesignationDefOf.EngraveWall);
                    result = false;
                }
                else
                {
                    LocalTargetInfo target = edifice;
                    if (pawn.CanReserve(target, 1, -1, null, forced))
                    {
                        target = c;
                        if (pawn.CanReserve(target, 1, -1, null, forced))
                        {
                            return true;
                        }
                    }
                    result = false;
                }
            }
            return result;
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return new Job(JobDefOf.EngraveWall, c.GetEdifice(pawn.Map));
        }
    }
}