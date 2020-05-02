﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace RP0
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class ModuleShowInfoUpdater : MonoBehaviour
    {
        protected bool run = true;
        private void Update()
        {
            if (run)
            {
                EntryCostDatabaseAccessor.Init();
                EntryCostDatabaseAccessor.GetFields();
//                EntryCostDatabaseAccessor.ScanDatabase();
                foreach (AvailablePart ap in EntryCostDatabaseAccessor.nameToPart.Values)
                {
                    if (ap.partPrefab.FindModuleImplementing<ModuleShowInfo>() is ModuleShowInfo msi)
                    {
                        foreach (AvailablePart.ModuleInfo x in ap.moduleInfos)
                        {
                            if (x.moduleName.Equals(ModuleShowInfo.sModuleName))
                            {
                                x.info = msi.GetInfo();
                            }
                        }
                    }
                }

                run = false;
                GameObject.Destroy(this);
            }
        }
    }

    public class EntryCostDatabaseAccessor
    {
        public static Dictionary<string, RealFuels.PartEntryCostHolder> holders = null;
        public static Dictionary<string, AvailablePart> nameToPart = null;
        private static FieldInfo holdersField = null, nameToPartField = null;
        private static bool initialized = false;

        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            if (AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == "RealFuels") is var RealFuelsAssembly &&
                RealFuelsAssembly.assembly.GetType("RealFuels.EntryCostDatabase") is Type rfEntryCostDatabaseType)
            {
                holdersField = rfEntryCostDatabaseType.GetField("holders", BindingFlags.NonPublic | BindingFlags.Static);
                nameToPartField = rfEntryCostDatabaseType.GetField("nameToPart", BindingFlags.NonPublic | BindingFlags.Static);
            } 
        }

        public static void GetFields()
        {
            holders = (Dictionary<string, RealFuels.PartEntryCostHolder>)holdersField.GetValue(null);
            nameToPart = (Dictionary<string, AvailablePart>)nameToPartField.GetValue(null);
        }

        public static RealFuels.PartEntryCostHolder GetHolder(string s)
        {
            if (holders is null) return null;
            if (!holders.TryGetValue(s, out RealFuels.PartEntryCostHolder val))
            {
                // Debug.LogWarning($"EntryCostModifierDatabase missing ECM PartHolder for {s}");
            }
            return val;
        }

        public static int GetCost(RealFuels.PartEntryCostHolder h, bool clearTracker=true)
        {
            if (clearTracker) RealFuels.EntryCostDatabase.ClearTracker();
            return h.GetCost();
        }

        public static bool IsUnlocked(string name) => RealFuels.EntryCostDatabase.IsUnlocked(name);
        private static string Color(bool unlocked) => unlocked ? "<color=green>" : null;
        private static string Uncolor(bool unlocked) => unlocked ? "</color>" : null;

        private static string CostString(RealFuels.PartEntryCostHolder h, int cost=-1)
        {
            if (cost == -1) cost = GetCost(h);
            bool unlocked = IsUnlocked(h.name);
            string sCost = !unlocked ? $": {cost}" : null;
            return $"{Color(unlocked)}{h.name}{sCost}{Uncolor(unlocked)}";
        }

        public static string DisplayHolder(RealFuels.PartEntryCostHolder h, bool recurse=false)
        {
            if (h is null) return "null";
            if (h.cost == 0 && h.children.Count == 1) return DisplayHolder(GetHolder(h.children.First()), recurse);
            string s = string.Empty;
            foreach (string child in h.children)
            {
                if (GetHolder(child) is RealFuels.PartEntryCostHolder childHolder)
                    s += $" | {CostString(childHolder)}";
            }
            // Append the recursive scan after building the top level, instead of during.
            if (recurse)
            {
                foreach (string child in h.children)
                {
                    if (GetHolder(child) is RealFuels.PartEntryCostHolder childHolder)
                        s += $"[{DisplayHolder(childHolder, recurse)}]";
                }
            }

            return $"{CostString(h, h.cost)}{s}";
        }

        public static void ScanDatabase()
        {
            if (holders != null)
            {
                foreach (var x in holders)   // Recurse through the database to touch all holders.
                {
                    DisplayHolder(x.Value, true);
                }
            }
        }
    }

    public class ModuleShowInfo : PartModule
    {
        public const string sModuleName = "Show Info";
        public const string dragCubeGroup = "Drag Cubes";

        [KSPField(guiName = "X+", groupName = dragCubeGroup, groupDisplayName = dragCubeGroup)] private string XP;
        [KSPField(guiName = "X-", groupName = dragCubeGroup)] private string XN;
        [KSPField(guiName = "Y+", groupName = dragCubeGroup)] private string YP;
        [KSPField(guiName = "Y-", groupName = dragCubeGroup)] private string YN;
        [KSPField(guiName = "Z+", groupName = dragCubeGroup)] private string ZP;
        [KSPField(guiName = "Z-", groupName = dragCubeGroup)] private string ZN;
        [KSPField(guiName = "Area", groupName = dragCubeGroup)] private string Area;
        [KSPField(guiName = "Cross Sectional Area", groupName = dragCubeGroup)] private string CrossSectionalArea;
        [KSPField(guiName = "Exposed Area", groupName = dragCubeGroup)] private string ExposedArea;
        [KSPField(guiName = "Occlusion Mult", groupName = dragCubeGroup)] private string OcclusionMultiplier;
        [KSPField(guiName = "Post-Occlusion Area", groupName = dragCubeGroup)] private string PostOcclusionArea;
        [KSPField(guiName = "Drag Coeff", groupName = dragCubeGroup)] private string DragCoefficient;
        [KSPField(guiName = "Part Aeroddynamic Area", groupName = dragCubeGroup)] private string PartAerodynamicArea;
        [KSPField(guiName = "Part Exposed Area", groupName = dragCubeGroup)] private string PartExposedArea;
        [KSPField(guiName = "Part SKinExposed Area", groupName = dragCubeGroup)] private string PartSkinExposedArea;
        [KSPField(guiName = "Part SkinExposed Fraction", groupName = dragCubeGroup)] private string PartSkinExposedFraction;
        [KSPField(guiName = "Part SkinThermalMass", groupName = dragCubeGroup)] private string PartSkinThermalMass;
        [KSPField(guiName = "PTD ConvectionMult", groupName = dragCubeGroup)] private string PTDConvectionMult;
        [KSPField(guiName = "PTD ConvectionArea", groupName = dragCubeGroup)] private string PTDConvectionArea;
        [KSPField(guiName = "PTD ConvectionCoeffMult", groupName = dragCubeGroup)] private string PTDConvectionCoeffMult;
        [KSPField(guiName = "PTD partPseudoRe", groupName = dragCubeGroup)] private string PTDPartPseudoRe;
        [KSPField(guiName = "PTD FinalCoeff", groupName = dragCubeGroup)] private string PTDFinalCoeff;


        [KSPField] public float updateInterval = 0.25f;

        private bool showCubeInfo = false;

        public override string GetModuleDisplayName() => "Unlock Requirements";

        public override string GetInfo()
        {
            EntryCostDatabaseAccessor.Init();
            EntryCostDatabaseAccessor.GetFields();

            string data = null, APInfo = null;
            if (EntryCostDatabaseAccessor.GetHolder(part.name) is RealFuels.PartEntryCostHolder h)
                data = $"Total cost: {EntryCostDatabaseAccessor.GetCost(h)}\n{EntryCostDatabaseAccessor.DisplayHolder(h, false)}";
            if (part.partInfo is AvailablePart ap)
                APInfo = $"Tech Required: {ap.TechRequired}";
            string res = $"Part name: {part.name}\n{APInfo}\n{data}";
            return res;
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
                StartCoroutine(DragCubeDisplay());
        }

        private System.Collections.IEnumerator DragCubeDisplay()
        {
            while (HighLogic.LoadedSceneIsFlight)
            {
                if (showCubeInfo != PhysicsGlobals.AeroDataDisplay)
                {
                    showCubeInfo = PhysicsGlobals.AeroDataDisplay;
                    Fields[nameof(XP)].guiActive = showCubeInfo;
                    Fields[nameof(XN)].guiActive = showCubeInfo;
                    Fields[nameof(YP)].guiActive = showCubeInfo;
                    Fields[nameof(YN)].guiActive = showCubeInfo;
                    Fields[nameof(ZP)].guiActive = showCubeInfo;
                    Fields[nameof(ZN)].guiActive = showCubeInfo;
                    Fields[nameof(Area)].guiActive = showCubeInfo;
                    Fields[nameof(CrossSectionalArea)].guiActive = showCubeInfo;
                    Fields[nameof(ExposedArea)].guiActive = showCubeInfo;
                    Fields[nameof(OcclusionMultiplier)].guiActive = showCubeInfo;
                    Fields[nameof(PostOcclusionArea)].guiActive = showCubeInfo;
                    Fields[nameof(PartAerodynamicArea)].guiActive = showCubeInfo;
                    Fields[nameof(PartExposedArea)].guiActive = showCubeInfo;
                    Fields[nameof(PartSkinExposedArea)].guiActive = showCubeInfo;
                    Fields[nameof(PartSkinExposedFraction)].guiActive = showCubeInfo;
                    Fields[nameof(DragCoefficient)].guiActive = showCubeInfo;
                    Fields[nameof(PartSkinThermalMass)].guiActive = showCubeInfo;
                    Fields[nameof(PTDConvectionMult)].guiActive = showCubeInfo;
                    Fields[nameof(PTDConvectionArea)].guiActive = showCubeInfo;
                    Fields[nameof(PTDConvectionCoeffMult)].guiActive = showCubeInfo;
                    Fields[nameof(PTDPartPseudoRe)].guiActive = showCubeInfo;
                    Fields[nameof(PTDFinalCoeff)].guiActive = showCubeInfo;
                }
                if (showCubeInfo)
                    BuildCubeData();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void BuildCubeData()
        {
            DragCubeList cubes = part.DragCubes;
            XP = $"{cubes.WeightedArea[0]:F2} ({cubes.GetCubeAreaDir(DragCubeList.GetFaceDirection(DragCube.DragFace.XP)):F2})";
            XN = $"{cubes.WeightedArea[1]:F2} ({cubes.GetCubeAreaDir(DragCubeList.GetFaceDirection(DragCube.DragFace.XN)):F2})";
            YP = $"{cubes.WeightedArea[2]:F2} ({cubes.GetCubeAreaDir(DragCubeList.GetFaceDirection(DragCube.DragFace.YP)):F2})";
            YN = $"{cubes.WeightedArea[3]:F2} ({cubes.GetCubeAreaDir(DragCubeList.GetFaceDirection(DragCube.DragFace.YN)):F2})";
            ZP = $"{cubes.WeightedArea[4]:F2} ({cubes.GetCubeAreaDir(DragCubeList.GetFaceDirection(DragCube.DragFace.ZP)):F2})";
            ZN = $"{cubes.WeightedArea[5]:F2} ({cubes.GetCubeAreaDir(DragCubeList.GetFaceDirection(DragCube.DragFace.ZN)):F2})";
            Area = $"{cubes.Area:F3}";
            CrossSectionalArea = $"{cubes.CrossSectionalArea:F3}";
            ExposedArea = $"{cubes.ExposedArea:F3}";
            OcclusionMultiplier = $"{cubes.OcclusionMultiplier:F3}";
            PostOcclusionArea = $"{cubes.PostOcclusionArea:F3}";
            PartAerodynamicArea = $"{part.aerodynamicArea:F3}";
            PartExposedArea = $"{part.exposedArea:F3}";
            PartSkinExposedArea = $"{part.skinExposedArea:F3}";
            PartSkinExposedFraction = $"{part.skinExposedAreaFrac:F3}";

            DragCoefficient = $"{cubes.DragCoeff:F3}";
            PartSkinThermalMass = $"{part.skinThermalMass:F3}";
            PTDConvectionMult = $"{part.ptd.convectionAreaMultiplier:F3}";
            PTDConvectionArea = $"{part.ptd.convectionArea:F3}";
            PTDConvectionCoeffMult = $"{part.ptd.convectionCoeffMultiplier:F3}";
            PTDPartPseudoRe = $"{part.ptd.partPseudoRe:F3}";
            PTDFinalCoeff = $"{part.ptd.finalCoeff:F3}";
        }
    }
}
