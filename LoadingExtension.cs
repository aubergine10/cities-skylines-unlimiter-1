using System;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using Object = UnityEngine.Object;

namespace EightyOne
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            if (loading == null || loading.currentMode == AppMode.Game)
            {
                Detours.SetUp();
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode != LoadMode.NewGame && mode != LoadMode.NewGameFromScenario && mode != LoadMode.LoadGame)
            {
                return;
            }
            Detours.Deploy();
            try
            {
                SimulationManager.instance.AddAction(() => Object.FindObjectOfType<RenderProperties>().m_edgeFogDistance = 2800f);
                SimulationManager.instance.AddAction(() => Object.FindObjectOfType<FogEffect>().m_edgeFogDistance = 2800f);
                SimulationManager.instance.AddAction(() => Object.FindObjectOfType<FogProperties>().m_EdgeFogDistance = 2800f);
            }
            catch
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("81 Tiles self-check: Mod conflict", "Another mod has altered edge fog effects; 81 Tiles was unable to reduce the edge fog.", false);
            }
            if (Util.IsModActive(Mod.ALL_TILES_START_MOD))
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(
                    "81 Tiles - Incompatible mod detected",
                    $"'81 Tiles' isn't compatible with 'All Tile Start' mod!\nPlease unsubscribe that mod and use '{Mod.UNLOCK_ALL_TILES_FOR_FREE}' button in '81 Tiles' options entry!",
                false);
            }
            SimulationManager.instance.AddAction(FixNotInIndustryAreaProblem);
            SimulationManager.instance.AddAction(FixNotInCampusAreaProblem);

            if (Enum.GetNames(typeof(DistrictPark.ParkType)).Length > 14)
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("81 Tiles self-check: new park types detected", "This new version of the game added new park types. 81 Tiles has to be updated in order to not get 'Not in X area' errors on save re-load", false);
            }
            
            if (Enum.GetNames(typeof(ImmaterialResourceManager.Resource)).Length > 30)
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("81 Tiles self-check: new resource types detected", "This new version of the game added new resource types. 81 Tiles has to be updated!", false);
            }
            
        }

        private static void FixNotInIndustryAreaProblem()
        {
            FixNotInParkProblem<IndustryBuildingAI>(Notification.Problem.NotInIndustryArea, GetIndustryPark);
        }
        
        private static void FixNotInCampusAreaProblem()
        {
            FixNotInParkProblem<CampusBuildingAI>(Notification.Problem.NotInCampusArea, GetCampusPark);
        }

        private static void FixNotInParkProblem<T>(Notification.Problem problem, Func<byte, Building, byte> getParkFunction) where T : PlayerBuildingAI
        {
            for (uint i = 0; i < BuildingManager.instance.m_buildings.m_size; i++)
            {
                var data = BuildingManager.instance.m_buildings.m_buffer[i];
                if (data.Info == null || !(data.Info.m_buildingAI is T))
                {
                    continue;
                }
                DistrictManager instance = Singleton<DistrictManager>.instance;
                if ((data.m_problems & problem) != Notification.Problem.None)
                {
                    byte park = instance.GetPark(data.m_position);
                    if (park != (byte) 0)
                    {
                        park = getParkFunction(park, data);
                    }

                    if (park != 0)
                    {
                        BuildingManager.instance.m_buildings.m_buffer[i].m_problems = Notification.RemoveProblems(data.m_problems,
                            problem);
                    }
                }
            }
        }

        private static byte GetIndustryPark(byte park, Building data)
        {
            DistrictManager instance = Singleton<DistrictManager>.instance;
            if (!instance.m_parks.m_buffer[(int) park].IsIndustry)
                park = (byte) 0;
            else
            {
                var industryBuildingAi = ((IndustryBuildingAI) data.Info.m_buildingAI);
                if (industryBuildingAi.m_industryType == DistrictPark.ParkType.Industry ||
                    industryBuildingAi.m_industryType != instance.m_parks.m_buffer[(int) park].m_parkType)
                    park = (byte) 0;
            }
            return park;
        }
        
        private static byte GetCampusPark(byte park, Building data)
        {
            DistrictManager instance = Singleton<DistrictManager>.instance;
            if (!instance.m_parks.m_buffer[(int) park].IsCampus)
                park = (byte) 0;
            else
            {
                var campusBuildingAi = ((CampusBuildingAI) data.Info.m_buildingAI);
                if (campusBuildingAi.m_campusType == DistrictPark.ParkType.GenericCampus ||
                    campusBuildingAi.m_campusType != instance.m_parks.m_buffer[(int) park].m_parkType)
                    park = (byte) 0;
            }
            return park;
        }

        public override void OnLevelUnloading()
        {
            Detours.Revert();
        }

        public override void OnReleased()
        {
            base.OnReleased();
            Detours.TearDown();
        }
    }
}
