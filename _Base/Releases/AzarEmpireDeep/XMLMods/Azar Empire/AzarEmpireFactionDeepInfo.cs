using Arcen.AIW2.Core;
using Arcen.AIW2.External.BulkPathfinding;
using Arcen.Universal;
using UnityEngine;
using System;

namespace Arcen.AIW2.External
{
    public sealed class AzarEmpireFactionDeepInfo : ExternalFactionDeepInfoRoot, IBulkPathfinding
    {
        public AzarEmpireFactionBaseInfo BaseInfo;
        public readonly List<SafeSquadWrapper> ConstructorsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed(150, "AzarEmpireDeepInfo-ConstructorsLRP");


        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AzarEmpireFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            BaseInfo = null;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        /*public override void SeedStartingEntities_LaterEverythingElse(Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            // instead, do the logic in update which handles everything
            if (BaseInfo.SpawnPlanet)
                return;

            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if (AttachedFaction.GetStringValueForCustomFieldOrDefaultValue("InvasionTime", true) == "Immediate")
            {
                AttachedFaction.HasDoneInvasionStyleAction = true;
                bool isSeeded = false;
                if (AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue("SpawnNearPlayer", true) ||
                     AttachedFaction.IsVassal)
                {
                    //put me on the player homeworld
                    //TODO: this is probably not the right thing to do in MP, especially if you have a suzerain?
                    GameEntityTypeData entityData = null;
                    World_AIW2.Instance.DoForEntities(EntityRollupType.KingUnitsOnly, delegate (GameEntity_Squad entity)
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, AzarEmpireFactionBaseInfo.Tag_AzarKing);
                        if (entity.GetFactionTypeSafe() == FactionType.Player)
                        {
                            entity.Planet.Mapgen_SeedEntity(Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere);
                            isSeeded = true;
                            return DelReturn.Break;
                        }
                        return DelReturn.Continue;
                    });
                }
                if (!isSeeded)
                {
                    StandardMapPopulator.Mapgen_SeedSpecialEntities(Context, galaxy, AttachedFaction, SpecialEntityType.None, NanocaustFactionBaseInfo.NANOCAUST_HIVE, SeedingType.HardcodedCount, 1,
                                                            MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal);
                }
            }

            // for(int i = 0; i < planetsSeededOn.Count; i++)
            // {
            //     //ArcenDebugging.ArcenDebugLogSingleLine("Nanocaust faction " + faction.FactionIndex + " hive seeded on " + planetsSeededOn[i].Name, Verbosity.DoNotShow );
            // }
            //StandardMapPopulator.ClearAllUnitsNotBelongingToThisFaction( planetsSeededOn, faction, true );
        }*/

        private Planet CreateSpawnPlanet(ArcenHostOnlySimContext hostCtx, Planet nearbyPlanet)
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( string.Format("[nano] CreateSpawnPlanet"), Verbosity.DoNotShow );

            var galaxy = World_AIW2.Instance.CurrentGalaxy;
            var rand = hostCtx.RandomToUse;

            int galaxy_radius;
            ArcenPoint galaxy_centroid;
            GetGalaxyRadius(out galaxy_radius, out galaxy_centroid);

            ArcenPoint planet_pnt = ArcenPoint.ZeroZeroPoint;
            int cur_radius = galaxy_radius / 4;
            int radius_step = galaxy_radius / 4;
            while (true)
            {
                // try 10 times at this radius
                // then increase the radius
                bool success = false;
                for (int i = 0; i < 10; i++)
                {
                    var pnt = RandomPointInRadius(rand, nearbyPlanet.GalaxyLocation, cur_radius);
                    if (!galaxy.CheckForTooCloseToExistingNodes(pnt, PlanetType.Normal, true))
                    {
                        planet_pnt = pnt;
                        success = true;
                        break;
                    }
                }

                if (success)
                    break;

                cur_radius += radius_step;
            }
            
            var planet = galaxy.AddPlanet(PlanetType.Normal, planet_pnt, PlanetGravWellSizeTable.Instance.GetRowByName("Vast")); 
            var playerLocalFaction = planet.GetFirstFactionOfType(FactionType.Player);
            playerLocalFaction.AIPLeftFromCommandStation = 0;
            playerLocalFaction.AIPLeftFromWarpGate = 0;

            var cmd = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.LinkPlanets], GameCommandSource.AnythingElse);
            cmd.RelatedIntegers.Add(planet.Index);
            cmd.RelatedIntegers.Add(nearbyPlanet.Index);
            World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, cmd, false);

            return planet;
        }

        private void GetGalaxyRadius(out int radius, out ArcenPoint centroid)
        {
            var galaxy = World_AIW2.Instance.CurrentGalaxy;

            int totalX = 0;
            int totalY = 0;
            int numPlanets = 0;
            galaxy.DoForPlanetsSingleThread(false,
                (p) =>
                {
                    totalX += p.GalaxyLocation.X;
                    totalY += p.GalaxyLocation.Y;
                    numPlanets++;
                    return DelReturn.Continue;
                });

            var center = ArcenPoint.Create(totalX / numPlanets, totalY / numPlanets);

            long maxLenSqr = 0;
            galaxy.DoForPlanetsSingleThread(false,
                (p) =>
                {
                    var d = p.GalaxyLocation.GetSquareDistanceTo(center);
                    if (d > maxLenSqr)
                        maxLenSqr = d;
                    return DelReturn.Continue;
                });

            radius = (int)Math.Sqrt(maxLenSqr);
            centroid = center;
        }

        ArcenPoint RandomPointInRadius(RandomGenerator rand, ArcenPoint pos, int radius)
        {
            var vec = Vector2.zero;

            for (int i = 0; i < 100; i++)
            {
                vec = new Vector2()
                {
                    x = rand.NextFloat(radius * 2) - radius,
                    y = rand.NextFloat(radius * 2) - radius,
                };

                var len = vec.magnitude;
                if (len > radius)
                    continue;

                vec.x += pos.X;
                vec.y += pos.Y;

                break;
            }

            return vec.ToArcenPoint();
        }

        /*public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // Before any custom logic, if they requested us be on a nomad planet, we're going on a nomad planet.
            if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "SeedOnNomadIfPossible", false ) ) //this field won't exist unless DLC2 is installed
            {
                //seed on a nomad if possible
                StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, BaseInfo.GetSphereTag(), SeedingType.HardcodedCount, 1, MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFactionOnNomadIfPossible, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
                return;
            }

            // Claimed Planets refers to Human/AI homeworlds, and other sphere planets.
            List<Planet> claimedPlanets = Planet.GetTemporaryPlanetList( "SphereFaction-SeedStartingEntities-claimedPlanets", 10f );
            List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "SphereFaction-SeedStartingEntities-potentialPlanets", 10f );
            bool seededNearbyFriendlySphere = false;
            World_AIW2.Instance.DoForPlanetsSingleThread( false, planet =>
            {
                GameEntity_Squad sphere = planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_DysonSphere, true, true );
                if ( sphere == null )
                    return DelReturn.Continue;

                claimedPlanets.AddIfNotAlreadyIn( planet );

                if ( sphere.TypeData.GetHasTag( SphereFactionBaseInfo.Tag_ZenithSphere ) || sphere.TypeData.GetHasTag( SphereFactionBaseInfo.Tag_SmallSphere ) && planet.OriginalHopsToHumanHomeworld <= 4 )
                    seededNearbyFriendlySphere = true;

                return DelReturn.Continue;
            } );

            World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnitsOnly, kingUnit =>
            {
                claimedPlanets.AddIfNotAlreadyIn( kingUnit.Planet );

                return DelReturn.Continue;
            } );

            // Build our potential planets list.
            // Before anything else, if we didn't seed a friendly sphere yet, and we're trying to seed a friendly sphere now; put it somewhat near the player homeworld.
            if ( BaseInfo.CanSphereBeFriendly && !seededNearbyFriendlySphere )
            {
                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnitsOnly, kingUnit =>
                {
                    if ( kingUnit.PlanetFaction.Faction.Type != FactionType.Player )
                        return DelReturn.Continue;

                    kingUnit.Planet.DoForPlanetsWithinXHops( 4, ( workingPlanet, workingPlanetHops ) =>
                    {
                        if ( workingPlanetHops < 3 || workingPlanet.MapGen_IsFullyUsedByAFaction )
                            return DelReturn.Continue;

                        potentialPlanets.Add( workingPlanet );

                        return DelReturn.Continue;
                    } );

                    return DelReturn.Continue;
                } );
            }

            if ( potentialPlanets.Count < 1 )
            {
                // We'll want to spread Dyson Spheres out equally in the galaxy. No overlapping. Equal spread from all homeworlds, and other spheres.
                // Obviously it's not going to be perfect; we aren't going to precalculate based on the number of sphere factions. We just want a bit of spread.
                // Start out requiring 6 hops, and decrease from there.
                for ( short x = 6; x > 0 && potentialPlanets.Count < 1; x-- )
                    World_AIW2.Instance.DoForPlanetsSingleThread( false, workingPlanet =>
                    {
                        bool isValid = !workingPlanet.MapGen_IsFullyUsedByAFaction;

                        if ( isValid )
                            for ( int i = 0; i < claimedPlanets.Count; i++ )
                            {
                                if ( workingPlanet.GetHopsTo( claimedPlanets[i] ) <= x )
                                {
                                    isValid = false;
                                    break;
                                }
                            }

                        if ( isValid )
                            potentialPlanets.AddIfNotAlreadyIn( workingPlanet );

                        return DelReturn.Continue;
                    } );
            }

            // Pick an actual planet, if possible. If not, just seed it normally with some generous restrictions.
            Planet planetToSeedOn = null;
            if ( potentialPlanets.Count > 0 )
                planetToSeedOn = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            if ( planetToSeedOn != null )
            {
                GameEntity_Squad sphere = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planetToSeedOn.GetPlanetFactionForFaction( AttachedFaction ), GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.GetSphereTag() ), 1, AttachedFaction.LooseFleet, 0, Engine_AIW2.Instance.CombatCenter, Context, "SeedingDysonSphere" );

                planetToSeedOn.MapGen_IsFullyUsedByAFaction = true;
                planetToSeedOn.MapGen_FullyUsingFaction = AttachedFaction;
                planetToSeedOn.MapGen_MajorFactionsBlockedHereReason = sphere.TypeData.DisplayName;
            }
            else
                StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None,
                BaseInfo.GetSphereTag(), SeedingType.HardcodedCount, 1, MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 3, 3,
                PlanetSeedingZone.InnerSystem, SeedingExpansionType.ComplicatedOriginal, null, -1 );

            Planet.ReleaseTemporaryPlanetList( claimedPlanets );
            Planet.ReleaseTemporaryPlanetList( potentialPlanets );
        }*/

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            //reset the faction Influences for this one
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList("AzarEmpire-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f);

            List<SafeSquadWrapper> basicFacilities = this.BaseInfo.BasicFacilities.GetDisplayList();
            List<SafeSquadWrapper> advancedFacilities = this.BaseInfo.AdvancedFacilities.GetDisplayList();
            Planet spherePlanet = BaseInfo.Sphere.Display?.Planet;
            if (spherePlanet != null)
                planetsInfluenced.AddIfNotAlreadyIn(spherePlanet);
            for (int i = 0; i < basicFacilities.Count; i++)
            {
                GameEntity_Squad entity = basicFacilities[i].GetSquad();
                planetsInfluenced.AddIfNotAlreadyIn(entity.Planet);
            }
            for (int i = 0; i < advancedFacilities.Count; i++)
            {
                GameEntity_Squad entity = advancedFacilities[i].GetSquad();
                planetsInfluenced.AddIfNotAlreadyIn(entity.Planet);
            }
            //Every influenced planet adds 20 AIP, but not for now
            //MinorFactionAIPEquivalentSet((FInt)(planetsInfluenced.Count * 20));
            AttachedFaction.SetInfluenceForPlanetsToList(planetsInfluenced);
            Planet.ReleaseTemporaryPlanetList(planetsInfluenced);
        }

        private readonly List<Planet> workingAllowedSpawnPlanets = List<Planet>.Create_WillNeverBeGCed(30, "AzarEmpireFactionDeepInfo-workingAllowedSpawnPlanets");

        #region Stage3
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if (!AttachedFaction.HasDoneInvasionStyleAction &&
                     (AttachedFaction.InvasionTime > 0 && AttachedFaction.InvasionTime <= World_AIW2.Instance.GameSecond))
            {
                //debugCode = 500;
                //Lets default to just putting the nanocaust hive on a completely random non-player non-ai-king planet
                //TODO: improve this
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, AzarEmpireFactionBaseInfo.Tag_AzarKing);

                Planet spawnPlanet = null;
                {
                    workingAllowedSpawnPlanets.Clear();
                    int preferredHomeworldDistance = 10;
                    do
                    {
                        //debugCode = 600;
                        World_AIW2.Instance.DoForPlanetsSingleThread(false, delegate (Planet planet)
                        {
                            //debugCode = 700;
                            if (this.BaseInfo.SeedNearPlayer && planet.GetControllingFactionType() == FactionType.Player)
                            {
                                workingAllowedSpawnPlanets.Add(planet);
                                return DelReturn.Continue;
                            }
                            if (planet.GetControllingFactionType() == FactionType.Player)
                                return DelReturn.Continue;
                            if (planet.GetFactionWithSpecialInfluenceHere().Type != FactionType.NaturalObject && preferredHomeworldDistance >= 6) //don't seed over a minor faction if we are finding good spots
                            {
                                return DelReturn.Continue;
                            }
                            if (planet.IsPlanetToBeDestroyed || planet.HasPlanetBeenDestroyed)
                                return DelReturn.Continue;
                            if (planet.PopulationType == PlanetPopulationType.AIBastionWorld ||
                                    planet.IsZenithArchitraveTerritory)
                            {
                                return DelReturn.Continue;
                            }
                            //debugCode = 800;
                            if (planet.OriginalHopsToAIHomeworld >= preferredHomeworldDistance &&
                                    (planet.OriginalHopsToHumanHomeworld == -1 ||
                                    planet.OriginalHopsToHumanHomeworld >= preferredHomeworldDistance))
                                workingAllowedSpawnPlanets.Add(planet);

                            return DelReturn.Continue;
                        });

                        preferredHomeworldDistance--;
                        if (preferredHomeworldDistance == 0)
                            break;
                    } while (workingAllowedSpawnPlanets.Count == 0);
                    //debugCode = 900;
                    if (workingAllowedSpawnPlanets.Count == 0)
                        throw new Exception("Unable to find a place to spawn the Azaran Empire");

                    // This is not actually random unless we set the seed ourselves.
                    // Since other processing happening before us tends to set the seed to the same value repeatedly.
                    Context.RandomToUse.ReinitializeWithSeed(Engine_Universal.PermanentQualityRandom.Next() + AttachedFaction.FactionIndex);
                    spawnPlanet = workingAllowedSpawnPlanets[Context.RandomToUse.Next(0, workingAllowedSpawnPlanets.Count)];

                    // always instead of spawning on this planet, create a new planet linked to it
                    spawnPlanet = CreateSpawnPlanet(Context, spawnPlanet);
                }

                PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction(AttachedFaction);
                ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts(0, 200), FInt.FromParts(0, 600));

                var azarKing = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, entityData.MarkFor(pFaction),
                                            pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AzarEmpireKing");
                AttachedFaction.HasDoneInvasionStyleAction = true;

                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>("ShipGeneralFocus");
                if (chatHandlerOrNull != null)
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create(azarKing);

                string planetStr = "";
                if (spawnPlanet.GetDoHumansHaveVision())
                {
                    planetStr = " from " + spawnPlanet.Name;
                }

                var str = string.Format("<color=#{0}>{1}</color> are invading{2}!", AttachedFaction.FactionCenterColor.ColorHexBrighter, AttachedFaction.GetDisplayName(), planetStr);
                World_AIW2.Instance.QueueChatMessageOrCommand(str, ChatType.LogToCentralChat, chatHandlerOrNull);
            }

            if ( BaseInfo.Sphere.Display != null )
            {
                if ( BaseInfo.Strength.Display >= BaseInfo.GetMaxStrength)
                {
                    BaseInfo.ClearBudget(); // We're full.
                    IncreaseBudgetForConstructorsWhenCapped( BaseInfo.GetPerSecondBudget );
                }
                else
                {
                    IncreaseBudget( BaseInfo.GetPerSecondBudget );
                    BaseInfo.BudgetForConstructorsWhenCapped = FInt.Zero;
                }
                SpawnUnitsIfAble(Context);
                if(BaseInfo.Sphere.Construction != null)
                {
                    if (BaseInfo.permanentBudgetFromAdvancedFacilities >= 20 || World_AIW2.Instance.GameSecond / 3600 >= 1)
                    {
                        BaseInfo.FactionMarkLevel = 2;
                        if (BaseInfo.Sphere.Construction.CurrentMarkLevel == 1)
                        {
                            BaseInfo.Sphere.Construction.SetCurrentMarkLevel(2);
                        }
                    }
                    if (BaseInfo.permanentBudgetFromAdvancedFacilities >= 60 || World_AIW2.Instance.GameSecond / 3600 >= 3)
                    {
                        BaseInfo.FactionMarkLevel = 3;
                        if (BaseInfo.Sphere.Construction.CurrentMarkLevel == 2)
                        {
                            BaseInfo.Sphere.Construction.SetCurrentMarkLevel(3);
                        }
                    }
                    if (BaseInfo.permanentBudgetFromAdvancedFacilities >= 120)
                    {
                        BaseInfo.FactionMarkLevel = 4;
                        if (BaseInfo.Sphere.Construction.CurrentMarkLevel == 3)
                        {
                            BaseInfo.Sphere.Construction.SetCurrentMarkLevel(4);
                        }
                    }
                    if (BaseInfo.permanentBudgetFromAdvancedFacilities >= 250)
                    {
                        BaseInfo.FactionMarkLevel = 5;
                        if (BaseInfo.Sphere.Construction.CurrentMarkLevel == 4)
                        {
                            BaseInfo.Sphere.Construction.SetCurrentMarkLevel(5);
                        }
                    }
                    if (BaseInfo.permanentBudgetFromAdvancedFacilities >= 500)
                    {
                        BaseInfo.FactionMarkLevel = 6;
                        if (BaseInfo.Sphere.Construction.CurrentMarkLevel == 5)
                        {
                            BaseInfo.Sphere.Construction.SetCurrentMarkLevel(6);
                        }
                    }
                    if (BaseInfo.permanentBudgetFromAdvancedFacilities >= 1000)
                    {
                        BaseInfo.FactionMarkLevel = 7;
                        if (BaseInfo.Sphere.Construction.CurrentMarkLevel == 6)
                        {
                            BaseInfo.Sphere.Construction.SetCurrentMarkLevel(7);
                        }
                    }
                }

                /*if ( BaseInfo.ShouldSpawnAntagonizer )
                    SpawnAntagonizer( Context );*/

                // Splintering Spire logic.
                /*if ( SplinteringSpireFactionBaseInfo.Instance != null )
                {
                    SplinteringSpireFactionBaseInfo.Instance.ControlByFactionOnDerelictPlanet.GetDisplayDict().DoFor( pair =>
                    {
                        pair.Value.DoFor( subPair =>
                        {
                            if ( subPair.Value > 0 )
                                HandleSplinteringSpirePerSecondState( subPair.Value );

                            return DelReturn.Continue;
                        } );

                        return DelReturn.Continue;
                    } );

                    if ( SplinteringSpireFactionBaseInfo.Instance.UnclaimedVictoryPoints.TryGetValue( AttachedFaction, out short wins ) && wins > 0 )
                    {
                        HandleSplinteringSpireVictoryPoint();
                        SplinteringSpireFactionBaseInfo.Instance.UnclaimedVictoryPoints.TryUpdate( AttachedFaction, (short)(wins - 1), wins );
                    }
                }*/
            }
        }

        //private void HandleSplinteringSpirePerSecondState( FInt percentage ) => IncreaseBudget( BaseInfo.GetPerSecondBudget * percentage );

        /*private void HandleSplinteringSpireVictoryPoint()
        {
            BaseInfo.AddedBudgetMultiplierFromExternalSources += SplinteringSpireFactionBaseInfo.Instance.Difficulty.RewardMultiplier;
            BaseInfo.AddedMaxStrengthMultiplierFromExternalSources += SplinteringSpireFactionBaseInfo.Instance.Difficulty.RewardMultiplier;
        }*/

        private void IncreaseBudget( FInt budgetToAdd )
        {
            //TODO
            if ( BaseInfo.Strength.Display < BaseInfo.GetMaxStrength / 10 ) // If we're super weak, due to early game or a recent loss, get mad.
                budgetToAdd *= BaseInfo.Difficulty.BudgetMultiplierWhenBelow10PercentCapacity;

            // Now, we split up our budget among our unit tiers. Lower tiers get more.
            //5 Tiers
            budgetToAdd /= 27;

            BaseInfo.ChangeStoredBudgetForTier( 0, budgetToAdd * 10, true );
            BaseInfo.ChangeStoredBudgetForTier( 1, budgetToAdd * 8, true );
            BaseInfo.ChangeStoredBudgetForTier( 2, budgetToAdd * 6, true );
            BaseInfo.ChangeStoredBudgetForTier( 3, budgetToAdd * 2, true);
            BaseInfo.ChangeStoredBudgetForTier( 4, budgetToAdd, true);
        }

        private void IncreaseBudgetForConstructorsWhenCapped(FInt budgetToAdd)
        {
            // For Constructors
            BaseInfo.BudgetForConstructorsWhenCapped += budgetToAdd;
        }

        private void SpawnUnitsIfAble( ArcenHostOnlySimContext context )
        {
            PlanetFaction pFaction = BaseInfo.Sphere.Display.PlanetFaction;

            // See if we can afford a unit for each tier.
            for ( byte x = 0; x < AzarEmpireFactionBaseInfo.MaxUnitTier; x++ )
            {
                if(BaseInfo.Strength.Display <= BaseInfo.GetMaxStrength)
                {
                    FInt currentBudget = BaseInfo.GetStoredBudgetForTier(x);
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(context, BaseInfo.GetUnitTagForTier(x));

                    int cost = entityData.CostForAIToPurchase; //Based only on AI cost since we're gonna mark up.
                    int canAfford = (currentBudget / cost).GetNearestIntPreferringLower();

                    int squadsToSpawn, stacksPerSquad = 0, extraUnitsToStack = 0;
                    if (canAfford <= 10)
                        squadsToSpawn = canAfford;
                    else
                    {
                        squadsToSpawn = 10;
                        stacksPerSquad = (canAfford - 10) / 10;
                        extraUnitsToStack = (canAfford - 10) % 10;
                    }

                    BaseInfo.ChangeStoredBudgetForTier(x, -(cost * canAfford), true);

                    // Spawn them in.
                    int strengthSpawned = 0;
                    for (byte y = 0; y < squadsToSpawn; y++)
                    {
                        // Don't let them overfill by TOO much.
                        if (BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength)
                            return;

                        ArcenPoint spawnPoint = BaseInfo.Sphere.Display.WorldLocation.GetRandomPointWithinDistance(context.RandomToUse, 0, 5000);
                        //TODO better mark spawning
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, (byte)BaseInfo.FactionMarkLevel,
                            pFaction.FleetUsedAtPlanet, 0, spawnPoint, context, "AzarEmpireFaction-MilitarySpawn");
                        if (entity != null)
                        {
                            entity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                            if (entity.TypeData.GetHasTag("AzarEmpireBasicConstructor") || entity.TypeData.GetHasTag("AzarEmpireAdvancedConstructor") || entity.TypeData.GetHasTag("AzarEmpireDefenseConstructor"))
                            {
                                //ArcenDebugging.ArcenDebugLogSingleLine("Entered the Tag of spawned unit if", Verbosity.ShowAsError);
                                TemplarPerUnitBaseInfo cData = entity.CreateExternalBaseInfo<TemplarPerUnitBaseInfo>("TemplarPerUnitBaseInfo");
                                //Telling the coloniser what we're gonna build
                                //cData.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, DireMacrophageFactionBaseInfo.DireTeliumTag);
                                //Planet newTeliumPlanet = GetNewTeliumLocation(coloniser, Context);
                                cData.UnitToBuild = null;
                                cData.PlanetIdx = -1;
                                cData.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                                //if (DireMacrophageFactionBaseInfo.debug)
                                //    ArcenDebugging.ArcenDebugLogSingleLine("Spawned a new coloniser " + coloniser.PrimaryKeyID + " for telium " + teliumID, Verbosity.DoNotShow);
                            }
                            strengthSpawned += entity.GetStrengthPerSquad();

                            // Don't let them overfill by TOO much.
                            if (BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength)
                                return;

                            for (int z = 0; z < stacksPerSquad; z++)
                            {
                                // Don't let them overfill by TOO much.
                                if (BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength)
                                    return;

                                entity.AddOrSetExtraStackedSquadsInThis(1, false);

                                strengthSpawned += entity.GetStrengthPerSquad();
                            }

                            if (extraUnitsToStack > 0)
                            {
                                // Don't let them overfill by TOO much.
                                if (BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength)
                                    return;

                                extraUnitsToStack--;
                                entity.AddOrSetExtraStackedSquadsInThis(1, false);

                                strengthSpawned += entity.GetStrengthPerSquad();
                            }
                        }
                    }
                }
                else
                {
                    if (BaseInfo.BudgetForConstructorsWhenCapped >= 15000)
                    {
                        BaseInfo.BudgetForConstructorsWhenCapped = FInt.Zero;
                        GameEntityTypeData entityData;
                        if (context.RandomToUse.Next(0, 100) < 25)
                        {
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(context, AzarEmpireFactionBaseInfo.Tag_AdvancedConstructor);
                        }
                        else
                        {
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(context, AzarEmpireFactionBaseInfo.Tag_BasicConstructor);
                        }
                        ArcenPoint spawnPoint = BaseInfo.Sphere.Display.WorldLocation.GetRandomPointWithinDistance(context.RandomToUse, 0, 5000);
                        //TODO better mark spawning
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, (byte)BaseInfo.FactionMarkLevel,
                            pFaction.FleetUsedAtPlanet, 0, spawnPoint, context, "AzarEmpireFaction-MilitarySpawn");
                        if (entity != null)
                        {
                            entity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                            if (entity.TypeData.GetHasTag("AzarEmpireBasicConstructor") || entity.TypeData.GetHasTag("AzarEmpireAdvancedConstructor") || entity.TypeData.GetHasTag("AzarEmpireDefenseConstructor"))
                            {
                                //ArcenDebugging.ArcenDebugLogSingleLine("Entered the Tag of spawned unit if", Verbosity.ShowAsError);
                                TemplarPerUnitBaseInfo cData = entity.CreateExternalBaseInfo<TemplarPerUnitBaseInfo>("TemplarPerUnitBaseInfo");
                                //Telling the coloniser what we're gonna build
                                //cData.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, DireMacrophageFactionBaseInfo.DireTeliumTag);
                                //Planet newTeliumPlanet = GetNewTeliumLocation(coloniser, Context);
                                cData.UnitToBuild = null;
                                cData.PlanetIdx = -1;
                                cData.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                                //if (DireMacrophageFactionBaseInfo.debug)
                                //    ArcenDebugging.ArcenDebugLogSingleLine("Spawned a new coloniser " + coloniser.PrimaryKeyID + " for telium " + teliumID, Verbosity.DoNotShow);
                            }
                        }
                    }
                }
            }
        }

        /*#region Old Zenith Dyson Sphere Compatibility
        // These functions are not the most optimized
        private void TakeSphereFromAntagonizedFaction( ArcenHostOnlySimContext context )
        {
            GameEntity_Squad antagonizedSphere = DysonSphereAntagonizedFactionBaseInfo.Instance.AttachedFaction.GetFirstMatching( "AntagonizedDysonSphere", true, true );
            if ( antagonizedSphere != null )
            {
                PlanetFaction pFaction = antagonizedSphere.Planet.GetPlanetFactionForFaction( AttachedFaction );
                GameEntity_Squad newSphere = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, SphereFactionBaseInfo.Tag_ZenithSphere ), 1,
                    pFaction.FleetUsedAtPlanet, 0, antagonizedSphere.WorldLocation, context, "ConvertingOldDysonSphere" );
                antagonizedSphere.Despawn( context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
            }
        }*/

        /*private void TakeUnitsFromAntagonizedFaction( ArcenHostOnlySimContext context )
        {
            int entitiesToTake = SphereFactionBaseInfo.OldDysonFactionsLeftToProcess > 1 ? DysonSphereAntagonizedFactionBaseInfo.Instance.AttachedFaction.GetTotalSquadCount() / SphereFactionBaseInfo.OldDysonFactionsLeftToProcess : 99999999; // Make sure we clean them out at the end.
            DysonSphereAntagonizedFactionBaseInfo.Instance.AttachedFaction.DoForEntities( "DysonSpawn", entity =>
            {
                if ( entitiesToTake <= 0 )
                    return DelReturn.Continue;

                EndpointFunctions.TransferEntityToFaction( entity, AttachedFaction, "ConvertingOldDysonUnit" );
                entitiesToTake--;

                return DelReturn.Continue;
            } );
        }*/
        #endregion

        /*public override void ReactToHacking_AsPartOfMainSim_HostOnly( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenHostOnlySimContext Context, HackingEvent Event, Faction overrideFaction = null )
        {
            //BaseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            // Add bonus budget equal to roughly a minute's worth of budget times the multiplier.
            FInt budgetToAdd = BaseInfo.GetPerSecondBudget;
            budgetToAdd *= 60;
            budgetToAdd *= WaveMultiplier;

            IncreaseBudget( budgetToAdd );
        }*/

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            BaseInfo.budgetFromBasicFacilities = 0;
            BaseInfo.maxStrengthFromBasicFacilities = 0;
            ConstructorsLRP.Clear();
            Planet spherePlanet = BaseInfo.Sphere.Display?.Planet;
            if ( spherePlanet == null )
                return;

            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_DefenseConstructor, delegate (GameEntity_Squad entity)
            {
                int debugCode = 0;
                int BuildRange = 250;
                //ArcenDebugging.ArcenDebugLogSingleLine("Entered the DoForEntities for Basic constructors", Verbosity.ShowAsError);
                ConstructorsLRP.Add(entity);
                try
                {
                    debugCode = 200;
                    //ArcenDebugging.ArcenDebugLogSingleLine("Entered the for", Verbosity.ShowAsError);
                    if (entity == null)
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("Fucker is null", Verbosity.ShowAsError);
                        return DelReturn.Continue;
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine("Past assignement of basic constructor", Verbosity.ShowAsError);
                    TemplarPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                    //ArcenDebugging.ArcenDebugLogSingleLine("past TemplarPerUnitBaseInfo get info", Verbosity.ShowAsError);
                    if (data.UnitToBuild == null)//If we forgot to set which unit to build
                    {
                        //this shouldn't be possible, but just in case
                        //ArcenDebugging.ArcenDebugLogSingleLine("before assigning unit to build", Verbosity.ShowAsError);
                        data.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, AzarEmpireFactionBaseInfo.Tag_Defense);
                        //ArcenDebugging.ArcenDebugLogSingleLine("assigning unit to build", Verbosity.ShowAsError);
                        return DelReturn.Continue;
                    }

                    if (data.PlanetIdx == -1)
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("no planet index found", Verbosity.ShowAsError);
                        //This shouldn't be possible, since we only create the constructor if there's a planet
                        //to build on, but just in case
                        Planet newCastlePlanet = GetNewDefenseLocation(entity, Context);
                        if (newCastlePlanet != null)
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("assigning planet to build on", Verbosity.ShowAsError);
                            data.PlanetIdx = newCastlePlanet.Index;
                            return DelReturn.Continue;
                        }
                        //we couldn't find a planet to build on, so just self-destruct
                        entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //we've created our new castle
                        return DelReturn.Continue;
                    }

                    if (entity.Planet.Index != data.PlanetIdx)
                        return DelReturn.Continue;
                    //ArcenDebugging.ArcenDebugLogSingleLine("At destination", Verbosity.ShowAsError);
                    debugCode = 300;
                    PlanetFaction pFaction = entity.PlanetFaction;
                    if (data.LocationToBuild == ArcenPoint.ZeroZeroPoint)
                    {
                        //if we have just arrived at our planet but don't have a location to build, pick one
                        data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundZone(Context, data.UnitToBuild, PlanetSeedingZone.MostAnywhere);
                        //data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundEntity(Context, data.UnitToBuild, entity, FInt.FromParts(0, 100), FInt.FromParts(0, 850));
                        return DelReturn.Continue;
                    }
                    debugCode = 400;
                    if (Mat.DistanceBetweenPointsImprecise(entity.WorldLocation, data.LocationToBuild) > BuildRange)
                        return DelReturn.Continue;
                    debugCode = 500;
                    ArcenPoint finalPoint = entity.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, data.UnitToBuild, data.LocationToBuild, FInt.FromParts(0, 10), FInt.FromParts(0, 50));
                    debugCode = 600;
                    //Checking for too much shit
                    //Homeworld can have more though, both base and based on Permanent budget bonus
                    List<SafeSquadWrapper> Defenses = this.BaseInfo.Defenses.GetDisplayList();
                    int DefensesAmount = 0;
                    for (int j = 0; j < Defenses.Count; j++)
                    {
                        if (Defenses[j].Planet == entity.Planet)
                        {
                            DefensesAmount++;
                        }
                    }
                    if (entity.Planet != BaseInfo.Sphere.Construction.Planet)
                    {
                        if (DefensesAmount >= (BaseInfo.BaseMaximumDefenses + (BaseInfo.permanentBudgetFromAdvancedFacilities / 30)) || DefensesAmount > 200)
                        {
                            data.UnitToBuild = null;
                            data.PlanetIdx = -1;
                            data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many advanced facilities already").Add("\n");
                            return DelReturn.Continue; ;
                        }
                    }
                    else
                    {//Note that that this allows 1 more than the formula on the if says, likely because of decials from comparing FINT
                        if (DefensesAmount >= (BaseInfo.BaseMaximumDefenses + (BaseInfo.permanentBudgetFromAdvancedFacilities / 15) + 16))
                        {
                            data.UnitToBuild = null;
                            data.PlanetIdx = -1;
                            data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                            //ArcenDebugging.ArcenDebugLogSingleLine("Facility amount fail", Verbosity.ShowAsError);
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many basic facilities already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    //Creating the new telium
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, data.UnitToBuild, (byte)BaseInfo.FactionMarkLevel,
                                                                                                  pFaction.Faction.LooseFleet, 0, finalPoint, Context, "AzarEmpire-DefenseConstructor");

                    debugCode = 700;
                    //Setting up its variables
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;

                    debugCode = 800;
                    debugCode = 900;
                    entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //we've created our new castle
                    debugCode = 1000;
                    ConstructorsLRP.Remove(entity);
                    debugCode = 100;
                }
                catch (Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleConstructorsSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
                }
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_BasicConstructor, delegate (GameEntity_Squad entity)
            {
                int debugCode = 0;
                int BuildRange = 250;
                //ArcenDebugging.ArcenDebugLogSingleLine("Entered the DoForEntities for Basic constructors", Verbosity.ShowAsError);
                ConstructorsLRP.Add(entity);
                try
                {
                    debugCode = 200;
                    //ArcenDebugging.ArcenDebugLogSingleLine("Entered the for", Verbosity.ShowAsError);
                    if (entity == null)
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("Fucker is null", Verbosity.ShowAsError);
                        return DelReturn.Continue;
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine("Past assignement of basic constructor", Verbosity.ShowAsError);
                    TemplarPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                    //ArcenDebugging.ArcenDebugLogSingleLine("past TemplarPerUnitBaseInfo get info", Verbosity.ShowAsError);
                    if (data.UnitToBuild == null)//If we forgot to set which unit to build
                    {
                        //this shouldn't be possible, but just in case
                        //ArcenDebugging.ArcenDebugLogSingleLine("before assigning unit to build", Verbosity.ShowAsError);
                        data.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, AzarEmpireFactionBaseInfo.Tag_BasicFacility);
                        //ArcenDebugging.ArcenDebugLogSingleLine("assigning unit to build", Verbosity.ShowAsError);
                        return DelReturn.Continue;
                    }

                    if (data.PlanetIdx == -1)
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("no planet index found", Verbosity.ShowAsError);
                        //This shouldn't be possible, since we only create the constructor if there's a planet
                        //to build on, but just in case
                        Planet newCastlePlanet = GetNewBasicFacilityLocation(entity, Context);
                        if (newCastlePlanet != null)
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("assigning planet to build on", Verbosity.ShowAsError);
                            data.PlanetIdx = newCastlePlanet.Index;
                            return DelReturn.Continue;
                        }
                        //we couldn't find a planet to build on, so just self-destruct
                        entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //we've created our new castle
                        return DelReturn.Continue;
                    }

                    if (entity.Planet.Index != data.PlanetIdx)
                        return DelReturn.Continue;
                    //ArcenDebugging.ArcenDebugLogSingleLine("At destination", Verbosity.ShowAsError);
                    debugCode = 300;
                    PlanetFaction pFaction = entity.PlanetFaction;
                    if (data.LocationToBuild == ArcenPoint.ZeroZeroPoint)
                    {
                        //if we have just arrived at our planet but don't have a location to build, pick one
                        data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundZone(Context, data.UnitToBuild, PlanetSeedingZone.MostAnywhere);
                        //data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundEntity(Context, data.UnitToBuild, entity, FInt.FromParts(0, 200), FInt.FromParts(0, 650));
                        return DelReturn.Continue;
                    }
                    debugCode = 400;
                    if (Mat.DistanceBetweenPointsImprecise(entity.WorldLocation, data.LocationToBuild) > BuildRange)
                        return DelReturn.Continue;
                    debugCode = 500;
                    ArcenPoint finalPoint = entity.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, data.UnitToBuild, data.LocationToBuild, FInt.FromParts(0, 10), FInt.FromParts(0, 50));
                    debugCode = 600;
                    //Checking for too much shit
                    //Homeworld can have more though, both base and based on Permanent budget bonus
                    List<SafeSquadWrapper> basicFacilities = this.BaseInfo.BasicFacilities.GetDisplayList();
                    int basicFacilitiesAmount = 0;
                    for (int j = 0; j < basicFacilities.Count; j++)
                    {
                        if (basicFacilities[j].Planet == entity.Planet)
                        {
                            basicFacilitiesAmount++;
                        }
                    }
                    if (entity.Planet != BaseInfo.Sphere.Construction.Planet)
                    {
                        if (basicFacilitiesAmount >= BaseInfo.BaseMaximumBasicFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 50) || basicFacilitiesAmount > 120)
                        {
                            data.UnitToBuild = null;
                            data.PlanetIdx = -1;
                            data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many advanced facilities already").Add("\n");
                            return DelReturn.Continue; ;
                        }
                    }
                    else
                    {//Note that that this allows 1 more than the formula on the if says, likely because of decials from comparing FINT
                        if (basicFacilitiesAmount >= (BaseInfo.BaseMaximumBasicFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 15) + 10))
                        {
                            data.UnitToBuild = null;
                            data.PlanetIdx = -1;
                            data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                            //ArcenDebugging.ArcenDebugLogSingleLine("Facility amount fail", Verbosity.ShowAsError);
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many basic facilities already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    //Creating the new telium
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, data.UnitToBuild, (byte)BaseInfo.FactionMarkLevel,
                                                                                                  pFaction.Faction.LooseFleet, 0, finalPoint, Context, "AzarEmpire-BasicConstructor");

                    debugCode = 700;
                    //Setting up its variables
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;

                    debugCode = 800;
                    debugCode = 900;
                    entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //we've created our new castle
                    debugCode = 1000;
                    ConstructorsLRP.Remove(entity);
                    debugCode = 100;
                }
                catch (Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleConstructorsSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
                }
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_AdvancedConstructor, delegate (GameEntity_Squad entity)
            {
                int debugCode = 0;
                int BuildRange = 250;
                ConstructorsLRP.Add(entity);
                try
                {
                    debugCode = 200;
                    if (entity == null)
                        return DelReturn.Continue;
                    TemplarPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                    if (data.UnitToBuild == null)//If we forgot to set which unit to build
                    {
                        //this shouldn't be possible, but just in case
                        data.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AzarEmpireAdvancedFacility");
                        return DelReturn.Continue;
                    }

                    if (data.PlanetIdx == -1)
                    {
                        //It's annoying to have that stuff in the spawner so I leave em empty and they come think here
                        Planet newCastlePlanet = GetNewAdvancedFacilityLocation(entity, Context);
                        if (newCastlePlanet != null)
                        {
                            data.PlanetIdx = newCastlePlanet.Index;
                            return DelReturn.Continue;
                        }
                        //we couldn't find a planet to build on, so just self-destruct
                        entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //we've created our new castle
                        return DelReturn.Continue;
                    }

                    if (entity.Planet.Index != data.PlanetIdx)
                        return DelReturn.Continue;
                    debugCode = 300;
                    PlanetFaction pFaction = entity.PlanetFaction;
                    if (data.LocationToBuild == ArcenPoint.ZeroZeroPoint)
                    {
                        //if we have just arrived at our planet but don't have a location to build, pick one
                        data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundZone(Context, data.UnitToBuild, PlanetSeedingZone.MostAnywhere);
                        //data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundEntity(Context, data.UnitToBuild, entity, FInt.FromParts(0, 200), FInt.FromParts(0, 650));
                        return DelReturn.Continue;
                    }
                    debugCode = 400;
                    if (Mat.DistanceBetweenPointsImprecise(entity.WorldLocation, data.LocationToBuild) > BuildRange)
                        return DelReturn.Continue;
                    debugCode = 500;
                    ArcenPoint finalPoint = entity.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, data.UnitToBuild, data.LocationToBuild, FInt.FromParts(0, 10), FInt.FromParts(0, 50));
                    debugCode = 600;
                    //Check whether or not there is too much shit
                    //Homeworld can have more though, both base and based on Permanent budget bonus
                    List<SafeSquadWrapper> advancedFacilities = this.BaseInfo.AdvancedFacilities.GetDisplayList();
                    int advancedFacilitiesAmount = 0;
                    for (int j = 0; j < advancedFacilities.Count; j++)
                    {
                        if (advancedFacilities[j].Planet == entity.Planet)
                        {
                            advancedFacilitiesAmount++;
                        }
                    }
                    if (entity.Planet != BaseInfo.Sphere.Construction.Planet)
                    {
                        if (advancedFacilitiesAmount >= BaseInfo.BaseMaximumAdvancedFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 100) || advancedFacilitiesAmount > 60)
                        {
                            data.UnitToBuild = null;
                            data.PlanetIdx = -1;
                            data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many advanced facilities already").Add("\n");
                            return DelReturn.Continue; ;
                        }
                    }
                    else
                    {//Note that that this allows 1 more than the formula on the if says, likely because of decials from comparing FINT
                        if (advancedFacilitiesAmount >= (BaseInfo.BaseMaximumAdvancedFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 30) + 4))
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("Facility amount fail", Verbosity.ShowAsError);
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many basic facilities already").Add("\n");
                            data.UnitToBuild = null;
                            data.PlanetIdx = -1;
                            data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                            return DelReturn.Continue;
                        }
                    }
                    //Creating the new telium
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, data.UnitToBuild, (byte)BaseInfo.FactionMarkLevel,
                                                                                                  pFaction.Faction.LooseFleet, 0, finalPoint, Context, "AzarEmpire-BasicConstructor");

                    debugCode = 700;
                    //Setting up its variables
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;

                    debugCode = 800;
                    debugCode = 900;
                    entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //we've created our new castle
                    debugCode = 1000;
                    ConstructorsLRP.Remove(entity);
                    debugCode = 100;
                }
                catch (Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleConstructorsSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
                }
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_GrantingBudgetBasic, delegate (GameEntity_Squad entity)
            {
                BaseInfo.budgetFromBasicFacilities += BaseInfo.Difficulty.BudgetPerSecond_OnProductionFacility.IntValue;
                return DelReturn.Continue;
            });
            AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_GrantingMaxStrengthBasic, delegate (GameEntity_Squad entity)
            {
                BaseInfo.maxStrengthFromBasicFacilities += BaseInfo.Difficulty.MaxStrength_IncreasePerStorageFacility.IntValue;
                return DelReturn.Continue;
            });
            //Checking the time, and if we went to another hour we add the bonuses
            int lastHourPermanentCheckInt = BaseInfo.lastHourPermanentCheck.ToInt();
            if (lastHourPermanentCheckInt < World_AIW2.Instance.GameSecond / 300)
            {
                BaseInfo.lastHourPermanentCheck = (World_AIW2.Instance.GameSecond / 300).ToFInt();
                AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_GrantingBudgetAdvancedPermanent, delegate (GameEntity_Squad entity)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Granting permanent budget", Verbosity.DoNotShow);
                    BaseInfo.permanentBudgetFromAdvancedFacilities += BaseInfo.Difficulty.BudgetPerSecond_IncreasePermanentPerInnovationFacility;
                    return DelReturn.Continue;
                });
                AttachedFaction.DoForEntities(AzarEmpireFactionBaseInfo.Tag_GrantingMaxStrengthAdvancedPermanent, delegate (GameEntity_Squad entity)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Granting permanent max strength", Verbosity.DoNotShow);
                    BaseInfo.permanentMaxStrengthFromAdvancedFacilities += BaseInfo.Difficulty.MaxStrength_IncreasePermanentPerCompressionFacility;
                    return DelReturn.Continue;
                });
            }

            HandleConstructorsLRP(Context, pathingCacheData);

            if ( ConflictPlanets == null )
                this.RebuildConflictPlanetsList();

            ConflictPlanets.Clear();

            /*if ( BaseInfo.IsAntagonized && BaseInfo.CanBeAntagonizedByAI )
            {
                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnitsOnly, king =>
                {
                    if ( king == null || king.PlanetFaction.Faction.Type != FactionType.Player )
                        return DelReturn.Continue;

                    ConflictPlanets.AddIfNotAlreadyIn( king.Planet );

                    return DelReturn.Continue;
                } );
            }*/
            /*else if ( BaseInfo.IsAntagonized && BaseInfo.CanBeAntagonizedByHumans && BaseInfo.DysonAntagonizer.Display != null )
            {
                if ( BaseInfo.DysonAntagonizer.Display.Planet != null )
                    ConflictPlanets.Add( BaseInfo.DysonAntagonizer.Display.Planet );
            }*/
            /*else if ( BaseInfo.IsCurrentlyAngryDueToHack )
            {
                if ( spherePlanet != null )
                    ConflictPlanets.Add( spherePlanet );
            }*/

            // If we aren't being antagonized by something in particular, simply find any conflict planets to journey towards.
            // Make sure we don't consider any planets that are outside of our hop limit.
            if (ConflictPlanets.Count == 0)
                this.RebuildConflictPlanetsList(null, 100, 100, false, false);
            //this.RebuildConflictPlanetsList( ( planet ) => planet.GetHopsTo( spherePlanet ) > BaseInfo.HopLimit );
            this.PrepareConflictPlanetMovementLogic(Context, 100, 30, entity => { return entity.TypeData.GetHasTag(AzarEmpireFactionBaseInfo.Tag_SkipForConflict); } );
            //this.PrepareConflictPlanetMovementLogic( Context, 100, 60, entity => { return entity.TypeData.GetHasTag( SplinteringSpireFactionBaseInfo.Tag_Collector ); } );

            this.ExecuteMovementCommands( Context );
            this.ExecuteWormholeCommands( Context );
            FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets(AttachedFaction, Context, 5f);

            pathingCacheData.ReturnToPool();
        }

        private static readonly List<Planet> WorkingPlanetsList = List<Planet>.Create_WillNeverBeGCed(100, "AzarEmpireDeepInfo-WorkingPlanetsList");

        private Planet GetNewBasicFacilityLocation(GameEntity_Squad entity, ArcenHostOnlySimContext Context)
        {
            ArcenDebugging.ArcenDebugLogSingleLine("Checking for a new location", Verbosity.DoNotShow);
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Templar);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("AzarEmpire-GetNewBasicFacilityLocation-trace", 10f) : null;

            List<SafeSquadWrapper> basicFacilities = this.BaseInfo.BasicFacilities.GetDisplayList();
            bool verboseDebug = true;
            WorkingPlanetsList.Clear();
            int debugCode = 0;
            try
            {
                //Whole galaxy hop range
                //ArcenDebugging.ArcenDebugLogSingleLine("starting the try", Verbosity.ShowAsError);
                entity.Planet.DoForPlanetsWithinXHops(-1, delegate (Planet planet, Int16 Distance)
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("inside the DoForPlanets", Verbosity.ShowAsError);
                    debugCode = 400;
                    if (tracing && verboseDebug)
                        tracingBuffer.Add("\tChecking whether we can build on " + planet.Name).Add("\n");
                    //This I care about, Basic Facilities only up to a limit based on the planet
                    //Homeworld can have more though, both base and based on Permanent budget bonus
                    int basicFacilitiesAmount = 0;
                    for (int i = 0; i < basicFacilities.Count; i++)
                    {
                        if (basicFacilities[i].Planet == planet)
                        {
                            basicFacilitiesAmount++;
                        }
                    }
                    if (planet.Index != BaseInfo.Sphere.Construction.Planet.Index)
                    {
                        if (basicFacilitiesAmount >= BaseInfo.BaseMaximumBasicFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 50) || basicFacilitiesAmount > 120)
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("Facility amount fail", Verbosity.ShowAsError);
                            if (tracing && verboseDebug)
                                tracingBuffer.Add("\t\tNo, too many basic facilities already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    else
                    {//Note that that this allows 1 more than the formula on the if says, likely because of decials from comparing FINT
                        if (basicFacilitiesAmount >= (BaseInfo.BaseMaximumBasicFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 15) + 10))
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("Facility amount fail", Verbosity.ShowAsError);
                            if (tracing && verboseDebug)
                                tracingBuffer.Add("\t\tNo, too many basic facilities already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    WorkingPlanetsList.Add(planet);
                    if (WorkingPlanetsList.Count > 10)
                    {
                        //Some choices, ideally close to the sphere
                        return DelReturn.Break;
                    }
                    return DelReturn.Continue;
                });

                if (WorkingPlanetsList.Count == 0)
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("Working Planet amount fail", Verbosity.ShowAsError);
                    return null;
                }
                //We prefer closer away planets
                /*WorkingPlanetsList.Sort(delegate (Planet L, Planet R)
                {
                    //sort from "closest" to "furthest away" (ideally)
                    int lHops = L.GetHopsTo(entity.Planet);
                    int rHops = R.GetHopsTo(entity.Planet);
                    return lHops.CompareTo(rHops);//to see if this actually works since I just swapped them so maybe it's useless
                });*/

                for (int i = 0; i < WorkingPlanetsList.Count; i++)
                {
                    //If we have the home as a potential candidate, no need for further debate
                    if(WorkingPlanetsList[i].Index == BaseInfo.Sphere.Construction.Planet.Index)
                    {
                        return WorkingPlanetsList[i];
                    }
                    if (Context.RandomToUse.Next(0, 100) < 40)
                        return WorkingPlanetsList[i];
                }
                return WorkingPlanetsList[0];
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetNewTTeliumLocation debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
            }
            return null;
        }

        private Planet GetNewDefenseLocation(GameEntity_Squad entity, ArcenHostOnlySimContext Context)
        {
            ArcenDebugging.ArcenDebugLogSingleLine("Checking for a new defense location", Verbosity.DoNotShow);
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Templar);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("AzarEmpire-GetNewDefenseLocation-trace", 10f) : null;

            List<SafeSquadWrapper> Defenses = this.BaseInfo.Defenses.GetDisplayList();
            bool verboseDebug = true;
            WorkingPlanetsList.Clear();
            int debugCode = 0;
            try
            {
                entity.Planet.DoForPlanetsWithinXHops(-1, delegate (Planet planet, Int16 Distance)
                {
                    debugCode = 400;
                    if (tracing && verboseDebug)
                        tracingBuffer.Add("\tChecking whether we can build on " + planet.Name).Add("\n");
                    //This I care about, Basic Facilities only up to a limit based on the planet
                    //Homeworld can have more though, both base and based on Permanent budget bonus
                    int DefensesAmount = 0;
                    for (int i = 0; i < Defenses.Count; i++)
                    {
                        if (Defenses[i].Planet == planet)
                        {
                            DefensesAmount++;
                        }
                    }
                    if (planet.Index != BaseInfo.Sphere.Construction.Planet.Index)
                    {
                        if (DefensesAmount >= (BaseInfo.BaseMaximumDefenses + (BaseInfo.permanentBudgetFromAdvancedFacilities / 30)) || DefensesAmount > 200)
                        {
                            if (tracing && verboseDebug)
                                tracingBuffer.Add("\t\tNo, too many defenses already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    else
                    {//Note that that this allows 1 more than the formula on the if says, likely because of decials from comparing FINT
                        if (DefensesAmount >= (BaseInfo.BaseMaximumDefenses + (BaseInfo.permanentBudgetFromAdvancedFacilities / 15) + 16))
                        {
                            if (tracing && verboseDebug)
                                tracingBuffer.Add("\t\tNo, too many defenses already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    WorkingPlanetsList.Add(planet);
                    if (WorkingPlanetsList.Count > 10)
                    {
                        //Some choices, ideally close to the sphere
                        return DelReturn.Break;
                    }
                    return DelReturn.Continue;
                });

                if (WorkingPlanetsList.Count == 0)
                {
                    return null;
                }

                for (int i = 0; i < WorkingPlanetsList.Count; i++)
                {
                    //If we have the home as a potential candidate, no need for further debate
                    if (WorkingPlanetsList[i].Index == BaseInfo.Sphere.Construction.Planet.Index)
                    {
                        return WorkingPlanetsList[i];
                    }
                    if (Context.RandomToUse.Next(0, 100) < 40)
                        return WorkingPlanetsList[i];
                }
                return WorkingPlanetsList[0];
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetNewTTeliumLocation debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
            }
            return null;
        }

        private Planet GetNewAdvancedFacilityLocation(GameEntity_Squad entity, ArcenHostOnlySimContext Context)
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Templar);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("AzarEmpire-GetNewAdvancedFacilityLocation-trace", 10f) : null;

            List<SafeSquadWrapper> advancedFacilities = this.BaseInfo.AdvancedFacilities.GetDisplayList();
            bool verboseDebug = false;
            WorkingPlanetsList.Clear();
            int debugCode = 0;
            try
            {
                entity.Planet.DoForPlanetsWithinXHops(-1, delegate (Planet planet, Int16 Distance)
                {
                    debugCode = 400;
                    if (tracing && verboseDebug)
                        tracingBuffer.Add("\tChecking whether we can build on " + planet.Name).Add("\n");
                    //This I care about, Advanced Facilities only up to a limit based on the planet
                    //Homeworld can have more though, both base and based on Permanent budget bonus
                    int advancedFacilitiesAmount = 0;
                    for (int i = 0; i < advancedFacilities.Count; i++)
                    {
                        if (advancedFacilities[i].Planet == planet)
                        {
                            advancedFacilitiesAmount++;
                        }
                    }
                    if (planet.Index != BaseInfo.Sphere.Construction.Planet.Index)
                    {
                        if (advancedFacilitiesAmount >= BaseInfo.BaseMaximumAdvancedFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 100) || advancedFacilitiesAmount > 60)
                        {
                            if (tracing && verboseDebug)
                                tracingBuffer.Add("\t\tNo, too many advanced facilities already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    else
                    {//Note that that this allows 1 more than the formula on the if says, likely because of decials from comparing FINT
                        if (advancedFacilitiesAmount >= (BaseInfo.BaseMaximumAdvancedFacilitiesPerPlanet + (BaseInfo.permanentBudgetFromAdvancedFacilities / 30) + 4))
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("Facility amount fail", Verbosity.ShowAsError);
                            //if (tracing && verboseDebug)
                            //    tracingBuffer.Add("\t\tNo, too many basic facilities already").Add("\n");
                            return DelReturn.Continue;
                        }
                    }
                    //Ballec mtn de ca
                    /*for (int i = 0; i < constructors.Count; i++)
                    {
                        GameEntity_Squad ship = constructors[i].GetSquad();
                        if (ship == null)
                            continue;
                        //make sure we don't have a constructor going here, so that the fuckers spread
                        TemplarPerUnitBaseInfo constructorData = ship.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                        if (constructorData.PlanetIdx == planet.Index)
                        {
                            if (tracing && verboseDebug)
                                tracingBuffer.Add("\t\tNo, we have a constructor en route already").Add("\n");

                            return DelReturn.Continue;
                        }
                    }*/

                    WorkingPlanetsList.Add(planet);
                    if (WorkingPlanetsList.Count > 10) //Some choices, ideally close to the sphere
                        return DelReturn.Break;
                    return DelReturn.Continue;
                });

                if (WorkingPlanetsList.Count == 0)
                    return null;
                //We prefer closer away planets
                /*WorkingPlanetsList.Sort(delegate (Planet L, Planet R)
                {
                    //sort from "closest" to "furthest away" (ideally)
                    int lHops = L.GetHopsTo(entity.Planet);
                    int rHops = R.GetHopsTo(entity.Planet);
                    return lHops.CompareTo(rHops);//to see if this actually works since I just swapped them so maybe it's useless
                });*/

                for (int i = 0; i < WorkingPlanetsList.Count; i++)
                {
                    //If we have the home as a potential candidate, no need for further debate
                    if (WorkingPlanetsList[i].Index == BaseInfo.Sphere.Construction.Planet.Index)
                    {
                        return WorkingPlanetsList[i];
                    }
                    if (Context.RandomToUse.Next(0, 100) < 40)
                        return WorkingPlanetsList[i];
                }
                return WorkingPlanetsList[0];
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetNewTTeliumLocation debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
            }
            return null;
        }

        public void HandleConstructorsLRP(ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData)
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                for (int i = 0; i < ConstructorsLRP.Count; i++)
                {
                    debugCode = 200;
                    GameEntity_Squad entity = ConstructorsLRP[i].GetSquad();
                    if (entity == null)
                        continue;
                    TemplarPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                    if (entity.HasQueuedOrders())
                        continue;
                    Planet destPlanet = World_AIW2.Instance.GetPlanetByIndex((short)data.PlanetIdx);
                    if (destPlanet == null)
                        continue; //should only be by race with sim
                    if (destPlanet != entity.Planet)
                        SendShipToPlanet(entity, destPlanet, Context, PathCacheData);//go to the planet
                    if (data.LocationToBuild == ArcenPoint.ZeroZeroPoint)
                        continue; //should only be by race with sim
                    SendShipToLocation(entity, data.LocationToBuild, Context);
                }
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleConstructorsLRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
            }
        }

        public void SendShipToPlanet(GameEntity_Squad entity, Planet destination, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData)
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache(entity.PlanetFaction.Faction, "AzarEmpireSendShipToPlanet", entity.Planet, destination, PathingMode.Shortest, Context, PathCacheData);
            if (pathCache != null && pathCache.PathToReadOnly.Count > 0)
            {
                GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse);
                command.RelatedString = "AzarEmpire_Dest";
                command.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                for (int k = 0; k < pathCache.PathToReadOnly.Count; k++)
                    command.RelatedIntegers.Add(pathCache.PathToReadOnly[k].Index);
                World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, command, false);
            }
        }

        public void SendShipToLocation(GameEntity_Squad entity, ArcenPoint dest, ArcenLongTermIntermittentPlanningContext Context)
        {
            GameCommand moveCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse);
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.RelatedPoints.Add(dest);
            moveCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
            World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, moveCommand, false);
        }
    }
}
