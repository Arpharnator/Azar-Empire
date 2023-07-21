using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AzarEmpireFactionDifficulty : ArcenDynamicTableRow, IConcurrentPoolable<AzarEmpireFactionDifficulty>, IProtectedListable
    {
        public string name;
        public int Intensity;

        public FInt BudgetPerSecond_Base;
        public FInt BudgetPerSecond_IncreasePerHour;
        public FInt BudgetPerSecond_OnProductionFacility;
        public FInt BudgetPerSecond_IncreasePermanentPerInnovationFacility;
        public FInt BudgetPerSecond_IncreasePer100AIP;
        public FInt BudgetPerSecond_MultiplierPerHack;

        public FInt MaxStrength_Base;
        public FInt MaxStrength_IncreasePerHour;
        public FInt MaxStrength_IncreasePerStorageFacility;
        public FInt MaxStrength_IncreasePermanentPerCompressionFacility;
        public FInt MaxStrength_IncreasePer100AIP;
        public FInt MaxStrength_MultiplierPerHack;

        public FInt BudgetMultiplierWhenBelow10PercentCapacity;

        //public FInt MaximumBasicFacilityPerPlanetBase;
        //public FInt MaximumAdvancedFacilityPerPlanetBase;

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AzarEmpireFactionDifficulty() : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker("AzarEmpireFactionDifficultys");
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<AzarEmpireFactionDifficulty> Pool = new ConcurrentPool<AzarEmpireFactionDifficulty>("AzarEmpireFactionDifficultys", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AzarEmpireFactionDifficulty(); } );

        public static AzarEmpireFactionDifficulty GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<AzarEmpireFactionDifficulty> typeAnalyzer;
        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<AzarEmpireFactionDifficulty>( new AzarEmpireFactionDifficulty() );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion
    }
    public class AzarEmpireFactionDifficultyTable : ArcenDynamicTable<AzarEmpireFactionDifficulty>
    {
        //Using a static instance variable -- the singleton pattern -- is okay here because this is a global data table.
        //Usage of singletons is NOT okay with the faction instance data, since you can have multiple instances of a faction and each should be unique.
        public static AzarEmpireFactionDifficultyTable Instance;
        public AzarEmpireFactionDifficultyTable() : base("AzarEmpireFactionDifficulty", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        public override AzarEmpireFactionDifficulty GetNewRowFromPool()
        {
            return AzarEmpireFactionDifficulty.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater()
        {
            //anything that is static on the table class and which  is not reset is going to have a bad time.
            //anything on the table class that is an instance variable will be blown away when this gets replaced
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, AzarEmpireFactionDifficulty TypeDataObject )
        {
            //bool debug = false;
            Data.Fill( "name", ref TypeDataObject.name, !Data.ReadingPartialRecord );
            Data.Fill( "intensity", ref TypeDataObject.Intensity, !Data.ReadingPartialRecord );

            Data.Fill( "BudgetPerSecond_Base", ref TypeDataObject.BudgetPerSecond_Base, true);
            Data.Fill( "BudgetPerSecond_IncreasePerHour", ref TypeDataObject.BudgetPerSecond_IncreasePerHour, true);
            Data.Fill( "BudgetPerSecond_OnProductionFacility", ref TypeDataObject.BudgetPerSecond_OnProductionFacility, true);
            Data.Fill( "BudgetPerSecond_IncreasePermanentPerInnovationFacility", ref TypeDataObject.BudgetPerSecond_IncreasePermanentPerInnovationFacility, true);
            Data.Fill( "BudgetPerSecond_IncreasePer100AIP", ref TypeDataObject.BudgetPerSecond_IncreasePer100AIP, true);
            Data.Fill( "BudgetPerSecond_MultiplierPerHack", ref TypeDataObject.BudgetPerSecond_MultiplierPerHack, true);

            Data.Fill( "MaxStrength_Base", ref TypeDataObject.MaxStrength_Base, true);
            Data.Fill( "MaxStrength_IncreasePerHour", ref TypeDataObject.MaxStrength_IncreasePerHour, true);
            Data.Fill( "MaxStrength_IncreasePerStorageFacility", ref TypeDataObject.MaxStrength_IncreasePerStorageFacility, true);
            Data.Fill( "MaxStrength_IncreasePermanentPerCompressionFacility", ref TypeDataObject.MaxStrength_IncreasePermanentPerCompressionFacility, true);
            Data.Fill( "MaxStrength_IncreasePer100AIP", ref TypeDataObject.MaxStrength_IncreasePer100AIP, true);
            Data.Fill( "MaxStrength_MultiplierPerHack", ref TypeDataObject.MaxStrength_MultiplierPerHack, true);

            Data.Fill( "BudgetMultiplierWhenBelow10PercentCapacity", ref TypeDataObject.BudgetMultiplierWhenBelow10PercentCapacity, true );

            //Data.Fill("MaximumBasicFacilityPerPlanetBase", ref TypeDataObject.MaximumBasicFacilityPerPlanetBase, true);
            //Data.Fill("MaximumAdvancedFacilityPerPlanetBase", ref TypeDataObject.MaximumAdvancedFacilityPerPlanetBase, true);


            if ( TypeDataObject.Intensity < 0 || TypeDataObject.Intensity > 10 )
                throw new Exception( "Unknown intensity for AzarEmpireFaction difficulty " + TypeDataObject.name + ", intensity " + TypeDataObject.Intensity );

            return DelReturn.Continue;
        }

        public AzarEmpireFactionDifficulty GetRowForFaction( Faction faction )
        {
            int factionIntensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            ArcenDebugging.ArcenDebugLogSingleLine("Successfully passed checking intensity", Verbosity.ShowAsError);

            AzarEmpireFactionDifficulty outputRow = null;
            for ( int i = 0; i < this.Rows.Count; i++ )
            {
                AzarEmpireFactionDifficulty row = this.Rows[i];
                if ( factionIntensity != row.Intensity )
                    continue;
                outputRow = row;
            }
            if ( outputRow == null )
            {
                throw new Exception("Could not find AzarEmpireFactionDifficultyDifficulty for " + faction.ToString() );
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetDifficultyForFaction: " + outputRow.name + ", as difficulty for " + faction.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.DoNotShow );
            return outputRow;
        }
        public AzarEmpireFactionDifficulty GetRowForIntensity(int factionIntensity)
        {
            AzarEmpireFactionDifficulty outputRow = null;
            for (int i = 0; i < this.Rows.Count; i++)
            {
                AzarEmpireFactionDifficulty row = this.Rows[i];
                if (factionIntensity != row.Intensity)
                    continue;
                outputRow = row;
            }
            if (outputRow == null)
            {
                throw new Exception("Could not find AzarEmpireFactionDifficultyDifficulty for Intensity " + factionIntensity.ToString());
            }
            bool debug = false;
            if (debug)
                ArcenDebugging.ArcenDebugLogSingleLine("GetRowForIntensity: " + outputRow.name + ", as difficulty for " + factionIntensity.ToString() + ", " + factionIntensity + "\n" + outputRow.ToString(), Verbosity.ShowAsError);
            return outputRow;
        }
    }
}
