using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AzarEmpireDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;
            AzarEmpireFactionBaseInfo baseInfo = RelatedEntityOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<AzarEmpireFactionBaseInfo>();
            if ( baseInfo == null )
                return;

            int perc = ((baseInfo.Strength.Display * 100) / baseInfo.GetMaxStrength).GetNearestIntPreferringHigher() ;
            Buffer.Add( $"This Command Center is currently supporting " + baseInfo.Strength.Display / 1000 + " / " + baseInfo.GetMaxStrength / 1000 + " maximum strength, with " + baseInfo.maxStrengthFromBasicFacilities / 1000 + " max strength from basic facilities and " + baseInfo.permanentMaxStrengthFromAdvancedFacilities / 1000 + " from permanent advancements \n");
            Buffer.Add( $"This Command Center's budget is " + baseInfo.GetPerSecondBudget + ", with " + baseInfo.budgetFromBasicFacilities + " from basic facilities and " + baseInfo.permanentBudgetFromAdvancedFacilities + " from permanent advancements.");

            if ( baseInfo.BudgetMultiplierFromHacks > FInt.One || baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                Buffer.Add("This Command Center's ");
                bool wroteAboutProduction = false;
                if ( baseInfo.BudgetMultiplierFromHacks > FInt.One ) {
                    Buffer.Add("production is <color=#a1ffa1>").Add( baseInfo.BudgetMultiplierFromHacks ).Add("x</color> normal");
                    wroteAboutProduction = true;
                }
                if ( baseInfo.MaxStrengthMultiplierFromHacks > FInt.One ) {
                    if (wroteAboutProduction) {
                        Buffer.Add( " and ");
                    }
                    Buffer.Add( "maximum strength is <color=#a1ffa1>").Add( baseInfo.MaxStrengthMultiplierFromHacks ).Add("x</color> normal");
                }
                Buffer.Add(". ");
            }
        }
    }
}
