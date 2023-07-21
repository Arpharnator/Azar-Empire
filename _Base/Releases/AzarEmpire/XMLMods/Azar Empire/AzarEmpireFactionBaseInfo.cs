using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AzarEmpireFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        public static readonly bool debug = false;
        // Important Values
        public int Intensity { get; private set; }
        //public string SphereType { get; private set; }
        public AzarEmpireFactionDifficulty Difficulty { get; private set; }

        #region Constants
        //public const string FactionField_SphereType = "SphereType";

        public const string Tag_AzarKing = "AzarEmpireKing";
        public const string Tag_DefenseConstructor = "AzarEmpireDefenseConstructor";
        public const string Tag_BasicConstructor = "AzarEmpireBasicConstructor";
        public const string Tag_AdvancedConstructor = "AzarEmpireAdvancedConstructor";
        public const string Tag_Defense = "AzarEmpireDefense";
        public const string Tag_BasicFacility = "AzarEmpireBasicFacility";
        public const string Tag_AdvancedFacility = "AzarEmpireAdvancedFacility";
        public const string Tag_GrantingBudgetBasic = "AzarEmpireBudgetBasicFacility";
        public const string Tag_GrantingMaxStrengthBasic = "AzarEmpireMaxStrengthBasicFacility";
        public const string Tag_GrantingBudgetAdvancedPermanent = "AzarEmpireBudgetAdvancedFacility";
        public const string Tag_GrantingMaxStrengthAdvancedPermanent = "AzarEmpireMaxStrengthAdvancedFacility";
        public const string Tag_SkipForConflict = "SkipForConflict";

        public const byte MaxUnitTier = 5;
        #endregion

        #region Serialized
        private readonly List<FInt> BudgetByTier = List<FInt>.Create_WillNeverBeGCed( 5, "AzarEmpireFactionBaseInfo-BudgetByTier", 1 );
        public FInt BudgetForConstructorsWhenCapped;
        public FInt AddedBudgetMultiplierFromExternalSources;
        public FInt AddedMaxStrengthMultiplierFromExternalSources;
        public FInt permanentBudgetFromAdvancedFacilities;
        public FInt permanentMaxStrengthFromAdvancedFacilities;
        public FInt MaximumAdvancedFacilitiesPerPlanet;
        public FInt MaximumBasicFacilitiesPerPlanet;
        public FInt lastHourPermanentCheck;
        #endregion

        #region Non Serialized
        public bool humanAllied = false;
        public bool aiAllied = false;
        public bool SeedNearPlayer = false;
        public DoubleBufferedValue<GameEntity_Squad> Sphere = new DoubleBufferedValue<GameEntity_Squad>( null );
        public DoubleBufferedValue<int> Strength = new DoubleBufferedValue<int>( 0 );
        public DoubleBufferedList<SafeSquadWrapper> MilitaryUnits = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "AzarEmpireFaction-BaseInfo-MilitaryUnits" );
        public DoubleBufferedList<Planet> PlanetsWithUnits = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 20, "AzarEmpireFaction-BaseInfo-PlanetsWithUnits" );
        //public DoubleBufferedValue<GameEntity_Squad> DysonAntagonizer = new DoubleBufferedValue<GameEntity_Squad>( null );
        public readonly DoubleBufferedList<SafeSquadWrapper> DefenseConstructors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(200, "AzarEmpire-DefenseConstructors");
        public readonly DoubleBufferedList<SafeSquadWrapper> BasicConstructors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(200, "AzarEmpire-BasicConstructors");
        public readonly DoubleBufferedList<SafeSquadWrapper> AdvancedConstructors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(200, "AzarEmpire-AdvancedConstructors");
        public readonly DoubleBufferedList<SafeSquadWrapper> Defenses = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(200, "AzarEmpire-Defenses");
        public readonly DoubleBufferedList<SafeSquadWrapper> BasicFacilities = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(200, "AzarEmpire-BasicFacilities");
        public readonly DoubleBufferedList<SafeSquadWrapper> AdvancedFacilities = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(200, "AzarEmpire-AdvancedFacilities");

        public int FactionMarkLevel = 1;
        public int budgetFromBasicFacilities = 0;
        public int maxStrengthFromBasicFacilities = 0;
        public int BaseMaximumAdvancedFacilitiesPerPlanet = 1;
        public int BaseMaximumBasicFacilitiesPerPlanet = 3;
        public int BaseMaximumDefenses = 8;


        //private bool PlayerIsNear;

        //public static int OldDysonFactionsLeftToProcess = 0;

        // Following can't change once determined. Regenerate on load to avoid serializing strings.
        private string cachedValue_globalUnitTag;
        private string cachedValue_tier0UnitTag;
        private string cachedValue_tier1UnitTag;
        private string cachedValue_tier2UnitTag;
        private string cachedValue_tier3UnitTag;
        private string cachedValue_tier4UnitTag;
        private string cachedValue_sphereTag;
        #endregion

        #region Budget Access
        public void ClearBudget()
        {
            BudgetByTier.Clear();
            while ( BudgetByTier.Count < MaxUnitTier )
                BudgetByTier.Add( FInt.Zero );
        }
        public FInt GetStoredBudgetForTier( int tier )
        {
            if ( tier >= MaxUnitTier )
                ArcenDebugging.ArcenDebugLog( $"Attempted to get Sphere budget for tier {tier}, max is {MaxUnitTier - 1}.", Verbosity.ShowAsError );
            return BudgetByTier[tier];
        }
        public void ChangeStoredBudgetForTier( int tier, int value, bool addToInsteadOfChange = false )
        {
            if ( tier >= MaxUnitTier )
                ArcenDebugging.ArcenDebugLog( $"Attemped to get Sphere budget for tier {tier}, max is {MaxUnitTier - 1}.", Verbosity.ShowAsError );
            if ( addToInsteadOfChange )
                BudgetByTier[tier] += value;
            else
                BudgetByTier[tier] = FInt.Zero + value;
        }
        public void ChangeStoredBudgetForTier( int tier, FInt value, bool addToInsteadOfChange = false )
        {
            if ( tier >= MaxUnitTier )
                ArcenDebugging.ArcenDebugLog( $"Attemped to get Sphere budget for tier {tier}, max is {MaxUnitTier - 1}.", Verbosity.ShowAsError );
            if ( addToInsteadOfChange )
                BudgetByTier[tier] += value;
            else
                BudgetByTier[tier] = value;
        }
        #endregion

        #region Budget/Max Strength Calculations

        public FInt PerSecondBudgetBeforeMultiplier => Difficulty.BudgetPerSecond_Base +
            (Difficulty.BudgetPerSecond_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (Difficulty.BudgetPerSecond_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600) +
            permanentBudgetFromAdvancedFacilities +
            budgetFromBasicFacilities);
        public FInt BudgetMultiplierFromHacks => FInt.One + AddedBudgetMultiplierFromExternalSources;
        public FInt GetPerSecondBudget => PerSecondBudgetBeforeMultiplier * BudgetMultiplierFromHacks;

        public FInt MaxStrengthBeforeMultiplier => Difficulty.MaxStrength_Base +
            (Difficulty.MaxStrength_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (Difficulty.MaxStrength_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600) +
            permanentMaxStrengthFromAdvancedFacilities +
            maxStrengthFromBasicFacilities);

        public FInt MaxStrengthMultiplierFromHacks => FInt.One + AddedMaxStrengthMultiplierFromExternalSources;
        public FInt GetMaxStrength
        {
            get
            {
                FInt baseMaxStrength = MaxStrengthBeforeMultiplier;
                FInt strength = baseMaxStrength * MaxStrengthMultiplierFromHacks;
                return strength;
            }
        }
        // Used for Hacking Estimates
        public FInt GetMaxStrengthWhileBeingHacked
        {
            get
            {
                FInt baseMaxStrength = MaxStrengthBeforeMultiplier;
                FInt strength = baseMaxStrength * MaxStrengthMultiplierFromHacks;
                if ( strength < baseMaxStrength * 2 )
                    strength = baseMaxStrength * 2; // Angry when hacked.
                return strength;
            }
        }
        #endregion

        #region Tags
        public string GetGlobalUnitTag() => cachedValue_globalUnitTag;
        public string GetUnitTagForTier( int tier )
        {
            switch ( tier )
            {
                case 0:
                    return cachedValue_tier0UnitTag;
                case 1:
                    return cachedValue_tier1UnitTag;
                case 2:
                    return cachedValue_tier2UnitTag;
                case 3:
                    return cachedValue_tier3UnitTag;
                default:
                    return cachedValue_tier4UnitTag;
            }
        }
        public string GetSphereTag() => cachedValue_sphereTag;
        #endregion

        #region Misc
        /*public bool CanSphereBeFriendly
        {
            get
            {
                switch ( SphereType )
                {
                    case SphereType_GraySpire:
                    case SphereType_Zenith:
                        return true;
                    default:
                        return false;
                }
            }
        }*/

        /*public int GetBaseHopLimit()
        {
            switch ( SphereType )
            {
                case SphereType_DarkSpire:
                    return 1;
                case SphereType_ChromaticSpire:
                    return 2;
                case SphereType_ImperialSpire:
                    return 1;
                case SphereType_GraySpire:
                    return 4;
                case SphereType_Zenith:
                    return 8; // Very far by default.
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( $"Unknown sphere type of {SphereType}", Verbosity.ShowAsError );
                    return -1;
            }
        }*/
        //public int NormalHopLimit => GetBaseHopLimit() + TimesHackedForHops;
        //public int HopLimit => IsAntagonized ? 999 : NormalHopLimit;
        #endregion

        #region Cleanup
        public AzarEmpireFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            Intensity = 0;
            Difficulty = null;
            humanAllied = false;
            aiAllied = false;
            SeedNearPlayer = false;

            ClearBudget();
            AddedBudgetMultiplierFromExternalSources = FInt.Zero;
            BudgetForConstructorsWhenCapped = FInt.Zero;
            AddedMaxStrengthMultiplierFromExternalSources = FInt.Zero;
            permanentBudgetFromAdvancedFacilities = FInt.Zero;
            permanentMaxStrengthFromAdvancedFacilities = FInt.Zero;
            MaximumAdvancedFacilitiesPerPlanet = FInt.Zero;
            MaximumBasicFacilitiesPerPlanet = FInt.Zero;
            budgetFromBasicFacilities = 0;
            maxStrengthFromBasicFacilities = 0;
            lastHourPermanentCheck = FInt.Zero;
            FactionMarkLevel = 1;

            Sphere.Clear();
            Strength.Clear();
            MilitaryUnits.Clear();
            PlanetsWithUnits.Clear();
            DefenseConstructors.Clear();
            BasicConstructors.Clear();
            AdvancedConstructors.Clear();
            BasicFacilities.Clear();
            Defenses.Clear();
            AdvancedFacilities.Clear();
            //PlayerIsNear = false;

            cachedValue_globalUnitTag = null;
            cachedValue_tier0UnitTag = null;
            cachedValue_tier1UnitTag = null;
            cachedValue_tier2UnitTag = null;
            cachedValue_tier3UnitTag = null;
            cachedValue_tier4UnitTag = null;
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)BudgetByTier.Count, "Budget Count" );
            BudgetByTier.ForEach( budget => Buffer.AddFInt( MetaData, budget, "Budget Value" ) );

            Buffer.AddFInt( MetaData, AddedBudgetMultiplierFromExternalSources, "Added Budget Multiplier From External Sources" );
            Buffer.AddFInt( MetaData, BudgetForConstructorsWhenCapped, "Budget For Constructors When Capped");
            Buffer.AddFInt( MetaData, AddedMaxStrengthMultiplierFromExternalSources, "Added Strength Multiplier From External Sources" );
            Buffer.AddFInt( MetaData, permanentBudgetFromAdvancedFacilities, "Added Budget Multiplier From External Sources");
            Buffer.AddFInt( MetaData, permanentMaxStrengthFromAdvancedFacilities, "Added Budget Multiplier From External Sources");
            Buffer.AddFInt( MetaData, MaximumAdvancedFacilitiesPerPlanet, "Maximum Advanced Facilities Per Planet");
            Buffer.AddFInt( MetaData, MaximumBasicFacilitiesPerPlanet, "Maximum Basic Facilities Per Planet");
            Buffer.AddFInt( MetaData, lastHourPermanentCheck, "Last Hour Check For Permanent Bonuses");
            
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            try
            {
                DoRefreshFromFactionSettings();
            }
            catch ( Exception )
            {
                // This is a harmless error, we simply want to do an initial refresh of settings to detect what type of sphere this faction is for to detect if it's Zenith, and from an old save. Skip it.
                //ArcenDebugging.ArcenDebugLogSingleLine( "Error when trying to load faction settings prior to Deserialization. We need to load this in order to detect prior Zenith factions, but loading it this early fails on Spire spheres. Error is not fatal, so is being skipped. If, however, you are having issues loading an old Dyson Sphere save, please report this error to Mantis; " + e.ToString(), Verbosity.DoNotShow );
            }
                ClearBudget();
                byte count = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Budget Count" );
                for ( byte x = 0; x < count; x++ )
                    if ( x < MaxUnitTier )
                        ChangeStoredBudgetForTier( x, Buffer.ReadFInt( MetaData, "Budget Value" ) );
                    else
                        Buffer.ReadFInt( MetaData, "Budget Value Over Tier Limit" );

                AddedBudgetMultiplierFromExternalSources = Buffer.ReadFInt( MetaData, "Percentage Budget Modifier From External Sources" );
                BudgetForConstructorsWhenCapped = Buffer.ReadFInt( MetaData, "Budget For Constructors When Capped");
                AddedMaxStrengthMultiplierFromExternalSources = Buffer.ReadFInt( MetaData, "Percentage Strength Modifier From External Sources" );
                permanentBudgetFromAdvancedFacilities = Buffer.ReadFInt( MetaData, "Permanent Budget from Advanced Facilities");
                permanentMaxStrengthFromAdvancedFacilities = Buffer.ReadFInt( MetaData, "Permanent Max Strength from Advanced Facilities");
                MaximumAdvancedFacilitiesPerPlanet = Buffer.ReadFInt( MetaData, "Maximum Advanced Facilities Per Planet");
                MaximumBasicFacilitiesPerPlanet = Buffer.ReadFInt( MetaData, "Maximum Basic Facilities Per Planet");
                lastHourPermanentCheck = Buffer.ReadFInt( MetaData, "Last Hour Check For Permanent Bonuses");

            #region Fixes
            // In this version, we accounted for an oversight that was causing Budget values to be generated faster than they could be spent.
            // Without this reset; it would result in potentially hundreds of ships spawning at once on load, and that's no good.
            if ( Buffer.FromGameVersion.GetLessThan( 3, 805 ) )
                    ClearBudget();
                #endregion
        }
        #endregion

        #region Faction Settings
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() => Intensity;

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused() { }

        private void LoadTagData()
        {
            cachedValue_globalUnitTag = "AzarEmpireSpawn";
            cachedValue_tier0UnitTag = "WeakAzarEmpireSpawn";
            cachedValue_tier1UnitTag = "MediumAzarEmpireSpawn";
            cachedValue_tier2UnitTag = "StrongAzarEmpireSpawn";
            cachedValue_tier3UnitTag = "VeryStrongAzarEmpireSpawn";
            cachedValue_tier4UnitTag = "PinnacleAzarEmpireSpawn";
            cachedValue_sphereTag = Tag_AzarKing;
        }

        protected override void DoRefreshFromFactionSettings()
        {
            // If we're a subfaction added by Splintering Spire, inherit their Intensity.
            Intensity = AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            Faction faction = this.AttachedFaction;
            string invasionTime = AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue("InvasionTime", true);
            this.SeedNearPlayer = AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue("SpawnNearPlayer", true);
            if (faction.InvasionTime == -1)
            {
                //initialize the invasion time
                if (invasionTime == "Immediate")
                    faction.InvasionTime = 1;
                else if (invasionTime == "Early Game")
                    faction.InvasionTime = (60 * 60); //1 hours in
                else if (invasionTime == "Mid Game")
                    faction.InvasionTime = (2 * (60 * 60)); //1.5 hours in
                else if (invasionTime == "Late Game")
                    faction.InvasionTime = 3 * (60 * 60); //3 hours in
                if (faction.InvasionTime > 1)
                {
                    //this will be a desync on the client and host, but the host will correct the client in under 5 seconds.
                    if (Engine_Universal.PermanentQualityRandom.Next(0, 100) < 50)
                        faction.InvasionTime += Engine_Universal.PermanentQualityRandom.Next(0, faction.InvasionTime / 10);
                    else
                        faction.InvasionTime -= Engine_Universal.PermanentQualityRandom.Next(0, faction.InvasionTime / 10);
                }
            }
            if ( Difficulty == null)
            {
                //GetRowForFaction can be a bitch if you haven't determined your intensity before it looks like
                Difficulty = AzarEmpireFactionDifficultyTable.Instance.GetRowForIntensity(AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue("Intensity", true));
            }
            if ( cachedValue_globalUnitTag == null )
                LoadTagData();
        }
        #endregion

        public override void SetStartingFactionRelationships() => AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );

        public override bool GetShouldAttackNormallyExcludedTarget(GameEntity_Squad Target)
        {
            try
            {
                Faction targetControllingFaction = Target.GetFactionOrNull_Safe();
                //bool planetHasWarpGate = false;
                //targetControllingFaction.DoForEntities( EntityRollupType.WarpEntryPoints, delegate ( GameEntity_Squad entity )
                //{
                //    if ( entity.Planet == Target.Planet )
                //        planetHasWarpGate = true;
                //    return DelReturn.Continue;
                //} );

                if (!ArcenStrings.Equals(this.Allegiance, "Friendly To Players"))
                {
                    //Human Allied factions will optionally leave an AI command station intact, so as to not drive AIP up so high
                    if (Target.TypeData.IsCommandStation)
                    {
                        return true;
                    }
                }
                if (Target.TypeData.GetHasTag("NormalPlanetNastyPick") || Target.TypeData.GetHasTag("DSAA"))
                    return true;
                //TODO VGs
                /*if (Target.TypeData.IsCommandStation && Sphere.Display != null && Sphere.Display.Planet == Target.Planet)
                    return true;*/

                return false;
            }
            catch (ArcenPleaseStopThisThreadException)
            {
                //this one is ok -- just means the thread is ending for some reason.  I guess we'll skip trying the normal handling here
                return false;
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLog("AzarEmpire GetShouldAttackNormallyExcludedTarget error:" + e, Verbosity.ShowAsError);
                return false;
            }
        }
        #region Notifier
        /*public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            // We'll want to get the count of annoyed and angry Sphere factions, as well as all respective timers.
            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();

            try
            {
                bool useSphereWorldValues = false;

                World_AIW2.Instance.DoForFactions( playerFaction =>
                {
                    if ( playerFaction.Type != FactionType.Player )
                        return DelReturn.Continue;

                    if ( Sphere.Display.Planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Self].TotalStrength >= GetStrengthToBeAnnoyedAt( true ) )
                    {
                        useSphereWorldValues = true;
                        return DelReturn.Break;
                    }

                    return DelReturn.Continue;
                } );

                int timeLeft = -1;

                fillData.Faction = AttachedFaction;
                fillData.BoolList.Add( useSphereWorldValues );
                fillData.BoolList.Add( DysonAntagonizer.Display != null );
                fillData.Int64List.Add( timeLeft );
                fillData.Int16List.Add( maxStrength );
                fillData.PlanetList.Add( Sphere.Display?.Planet );
                fillData.PlanetList.Add( DysonAntagonizer.Display?.Planet );
                else
                    fillData.StringList.Add( string.Empty ); // Zenith's name is built into its faction name.

                NotificationNonSim notification = new NotificationNonSim();
                notification.Assign( SphereFactionNotifier.Instance, fillData, "", 0, "Sphere Faction Notifier", SortedNotificationPriorityLevel.Medium );

            }
            catch ( Exception )
            {
                //ArcenDebugging.SingleLineQuickDebug("Failed Sphere Notifier");
                fillData.ReturnToPool();
            }

        }*/
        #endregion

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            Sphere.ClearConstructionValueForStartingConstruction();
            Strength.ClearConstructionValueForStartingConstruction();
            MilitaryUnits.ClearConstructionListForStartingConstruction();
            PlanetsWithUnits.ClearConstructionListForStartingConstruction();
            DefenseConstructors.ClearConstructionListForStartingConstruction();
            BasicConstructors.ClearConstructionListForStartingConstruction();
            AdvancedConstructors.ClearConstructionListForStartingConstruction();
            BasicFacilities.ClearConstructionListForStartingConstruction();
            Defenses.ClearConstructionListForStartingConstruction();
            AdvancedFacilities.ClearConstructionListForStartingConstruction();
            //DysonAntagonizer.ClearConstructionValueForStartingConstruction();

            Sphere.Construction = AttachedFaction.GetFirstMatching( GetSphereTag(), true, true );
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_DefenseConstructor, delegate (GameEntity_Squad entity)
            {
                DefenseConstructors.AddToConstructionList(entity);
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_BasicConstructor, delegate (GameEntity_Squad entity)
            {
                BasicConstructors.AddToConstructionList(entity);
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_AdvancedConstructor, delegate (GameEntity_Squad entity)
            {
                AdvancedConstructors.AddToConstructionList(entity);
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_BasicFacility, delegate (GameEntity_Squad entity)
            {
                BasicFacilities.AddToConstructionList(entity);
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_Defense, delegate (GameEntity_Squad entity)
            {
                Defenses.AddToConstructionList(entity);
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_AdvancedFacility, delegate (GameEntity_Squad entity)
            {
                AdvancedFacilities.AddToConstructionList(entity);
                return DelReturn.Continue;
            });

            AttachedFaction.DoForEntities( GetGlobalUnitTag(), entity =>
            {
                Strength.Construction += entity.GetStrengthOfSelfAndContents();
                MilitaryUnits.AddToConstructionList( entity );

                // See if the player is near any of our units.
                if ( !PlanetsWithUnits.ConstructionContains( entity.Planet ) )
                {
                    PlanetsWithUnits.AddToConstructionList( entity.Planet );

                    #region Near Player Check
                    /*
                    if ( !PlayerIsNear )
                        entity.Planet.DoForLinkedNeighborsAndSelf( false, adjPlanet =>
                        {
                            World_AIW2.Instance.DoForFactions( workingFaction =>
                            {
                                if ( workingFaction.Type != FactionType.Player )
                                    return DelReturn.Continue;

                                if ( adjPlanet.GetPlanetFactionForFaction( workingFaction ).DataByStance[FactionStance.Self].TotalStrength >= 1000 )
                                {
                                    PlayerIsNear = true;
                                    return DelReturn.Break;
                                }

                                return DelReturn.Continue;
                            } );

                            if ( PlayerIsNear )
                                return DelReturn.Break;

                            return DelReturn.Continue;
                        } );
                    */
                    #endregion
                }

                return DelReturn.Continue;
            } );

            Sphere.SwitchConstructionToDisplay();
            Strength.SwitchConstructionToDisplay();
            MilitaryUnits.SwitchConstructionToDisplay();
            PlanetsWithUnits.SwitchConstructionToDisplay();
            DefenseConstructors.SwitchConstructionToDisplay();
            BasicConstructors.SwitchConstructionToDisplay();
            AdvancedConstructors.SwitchConstructionToDisplay();
            BasicFacilities.SwitchConstructionToDisplay();
            Defenses.SwitchConstructionToDisplay();
            AdvancedFacilities.SwitchConstructionToDisplay();
            //DysonAntagonizer.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( Sphere.Display != null )
                UpdateAllegiance();
        }

        private void UpdateAllegiance()
        {
            {
                if (ArcenStrings.Equals(this.Allegiance, "Hostile To All") ||
                   ArcenStrings.Equals(this.Allegiance, "HostileToAll") ||
                   string.IsNullOrEmpty(this.Allegiance))
                {
                    this.humanAllied = false;
                    this.aiAllied = false;
                    if (string.IsNullOrEmpty(this.Allegiance))
                        throw new Exception("empty AzarEmpire allegiance '" + this.Allegiance + "'");
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This AzarEmpire faction should be hostile to all (default)", Verbosity.DoNotShow);
                    //make sure this isn't set wrong somehow
                    AllegianceHelper.EnemyThisFactionToAll(AttachedFaction);
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Hostile To Players Only") ||
                        ArcenStrings.Equals(this.Allegiance, "HostileToPlayers"))
                {
                    this.aiAllied = true;
                    AllegianceHelper.AllyThisFactionToAI(AttachedFaction);
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This AzarEmpire faction should be friendly to the AI and hostile to players", Verbosity.DoNotShow);

                }
                else if (ArcenStrings.Equals(this.Allegiance, "Minor Faction Team Red"))
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This AzarEmpire faction is on team red", Verbosity.DoNotShow);
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Minor Faction Team Red");
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Minor Faction Team Blue"))
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This AzarEmpire faction is on team blue", Verbosity.DoNotShow);

                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Minor Faction Team Blue");
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Minor Faction Team Green"))
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This AzarEmpire faction is on team green", Verbosity.DoNotShow);

                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Minor Faction Team Green");
                }

                else if (ArcenStrings.Equals(this.Allegiance, "HostileToAI") ||
                        ArcenStrings.Equals(this.Allegiance, "Friendly To Players"))
                {
                    this.humanAllied = true;
                    AllegianceHelper.AllyThisFactionToHumans(AttachedFaction);
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This AzarEmpire faction should be hostile to the AI and friendly to players", Verbosity.DoNotShow);
                }
                else
                {
                    throw new Exception("unknown AzarEmpire allegiance '" + this.Allegiance + "'");
                }
            }
        }

        /*private void UpdateAllegiance_Regular()
        {
            bool isBeingAnnoyedByPlayerOnSpherePlanet = false;
            bool isBeingAnnoyedByPlayerElsewhere = false;
            bool shouldStartOrContinueToBeAngryAtPlayer = false;

            int maxStrengthAtSphere = GetStrengthToBeAnnoyedAt( true );
            int maxStrengthOtherwise = GetStrengthToBeAnnoyedAt( false );

            // For every player faction, see if they're sticking their nose in our business more than we like.
            World_AIW2.Instance.DoForFactions( workingFaction =>
            {
                if ( workingFaction.Type != FactionType.Player )
                    return DelReturn.Continue;

                if ( isBeingAnnoyedByPlayerOnSpherePlanet || isBeingAnnoyedByPlayerElsewhere )
                    return DelReturn.Continue; // Another player is being rude to us already.

                // Sphere check first, its far more limiting.
                if ( Sphere.Display.Planet.GetPlanetFactionForFaction( workingFaction ).DataByStance[FactionStance.Self].TotalStrength >= maxStrengthAtSphere )
                    isBeingAnnoyedByPlayerOnSpherePlanet = true;

                // If we care about non sphere planets, and our sphere planet isn't agitated, check.
                if ( !isBeingAnnoyedByPlayerOnSpherePlanet && GetSecondsOfAnnoyanceUntilAngry( false ) > 0 )
                    PlanetsWithUnits.Display_DoFor( planet =>
                    {
                        if ( planet.GetPlanetFactionForFaction( workingFaction ).DataByStance[FactionStance.Self].TotalStrength >= maxStrengthOtherwise )
                        {
                            isBeingAnnoyedByPlayerElsewhere = true;
                            return DelReturn.Break;
                        }

                        return DelReturn.Continue;
                    } );

                return DelReturn.Continue;
            } );



            if ( isBeingAnnoyedByPlayerOnSpherePlanet )
            {
                SetIsCurrentlyBeingAnnoyed();
                if ( SecondsSinceBeingAnnoyed >= GetSecondsOfAnnoyanceUntilAngry( true ) )
                    shouldStartOrContinueToBeAngryAtPlayer = true;
            }
            else if ( isBeingAnnoyedByPlayerElsewhere )
            {
                SetIsCurrentlyBeingAnnoyed();
                if ( SecondsSinceBeingAnnoyed >= GetSecondsOfAnnoyanceUntilAngry( false ) )
                    shouldStartOrContinueToBeAngryAtPlayer = true;
            }
            else if ( IsCurrentlyAngry )
                SetIsCurrentlyBeingAnnoyed();
            else
                SetIsNotCurrentlyBeingAnnoyed();

            if ( shouldStartOrContinueToBeAngryAtPlayer )
                GameSecondLastAngered = World_AIW2.Instance.GameSecond;

            if ( IsCurrentlyAngry || IsCurrentlyAngryDueToHack || Sphere.Display?.Planet.GetControllingFactionType() == FactionType.AI )
                AllegianceHelper.EnemyThisFactionToAll( AttachedFaction ); // We want blood.
            else //if ( PlayerIsNear )
                AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
            //else
            //NeutralThisFactionToHumansAndTheirAllies();
        }

        private void NeutralThisFactionToHumansAndTheirAllies()
        {
            // Be allied to any player or player aligned faction that is near us, neutral otherwise.
            World_AIW2.Instance.DoForFactions( primaryFaction =>
            {
                if ( primaryFaction.Type != FactionType.Player )
                    return DelReturn.Continue;

                primaryFaction.MakeNeutralTo( AttachedFaction );
                AttachedFaction.MakeNeutralTo( primaryFaction );

                World_AIW2.Instance.DoForFactions( secondaryFaction =>
                {
                    if ( !primaryFaction.GetIsFriendlyTowards( secondaryFaction ) )
                        return DelReturn.Continue; // We only want friends of humans here.

                    secondaryFaction.MakeNeutralTo( AttachedFaction );
                    AttachedFaction.MakeNeutralTo( secondaryFaction );

                    return DelReturn.Continue;
                } );

                return DelReturn.Continue;
            } );
        }*/

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 10 + (Intensity * 6);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( $" Load From Azar Empire" );
            return load;
        }
        //TODO
        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt result = FInt.Zero;
            if (this.BasicFacilities.Count < 8)
            {
                result = FInt.FromParts(0, 000);
            }
            else if (this.BasicFacilities.Count < 20)
            {
                result = FInt.FromParts(0, 500);
            }
            else if (this.BasicFacilities.Count < 40)
            {
                result = FInt.FromParts(1, 000);
            }
            else if (this.BasicFacilities.Count < 60)
            {
                result = FInt.FromParts(2, 000);
            }
            else if (this.BasicFacilities.Count < 100)
            {
                result = FInt.FromParts(3, 000);
            }
            else if (this.BasicFacilities.Count < 140)
            {
                result = FInt.FromParts(4, 000);
            }
            else if (this.BasicFacilities.Count < 200)
            {
                result = FInt.FromParts(4, 500);
            }
            else
            {
                result = FInt.FromParts(5, 000);
            }
            this.AttachedFaction.OverallPowerLevel = result;
            //if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            //    ArcenDebugging.ArcenDebugLogSingleLine("resulting power level: " + faction.OverallPowerLevel, Verbosity.DoNotShow );
        }
        #endregion
    }
}
