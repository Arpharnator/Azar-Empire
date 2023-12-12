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

            FInt BaselineMaxStrength = baseInfo.Difficulty.MaxStrength_Base +
                (baseInfo.Difficulty.MaxStrength_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
                (baseInfo.Difficulty.MaxStrength_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600));

            FInt BaselineMaxBudget = baseInfo.Difficulty.BudgetPerSecond_Base +
            (baseInfo.Difficulty.BudgetPerSecond_IncreasePer100AIP * (FactionUtilityMethods.Instance.GetCurrentAIP() / 100)) +
            (baseInfo.Difficulty.BudgetPerSecond_IncreasePerHour * (World_AIW2.Instance.GameSecond / 3600));

            Buffer.BeginStatement(TextStyle.Get("Newline"));
            Buffer.Add($"Strength Capacity Status :", TextStyle.Attr_Label);
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).AddStrength_MoreReadable(baseInfo.Strength.Display, true).Add(" / ").AddStrength_MoreReadable(baseInfo.GetMaxStrength, true).Add(".").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.EndStatement(TextStyle.Get("Newline"));


            Buffer.BeginStatement(TextStyle.Get("Newline"));
            Buffer.Add($"Maximum Strength breakdown :", TextStyle.Attr_Label);
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).AddStrength_MoreReadable(baseInfo.maxStrengthFromBasicFacilities, true).Add(" from Basic Facilities.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).AddStrength_MoreReadable(baseInfo.permanentMaxStrengthFromAdvancedFacilities, true).Add(" from Permanent Advancements.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).AddStrength_MoreReadable(BaselineMaxStrength, true).Add(" Baseline.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).AddStrength_MoreReadable(baseInfo.GetMaxStrength, true).Add(" Total Maximum Strength.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.EndStatement(TextStyle.Get("Newline"));

            //Buffer.Add("\n\n");
            Buffer.BeginStatement(TextStyle.Get("Newline"));
            Buffer.Add($"Budget Income breakdown :", TextStyle.Attr_Label);
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).Add(baseInfo.budgetFromBasicFacilities, TextStyle.Get("GreenTextNoNewline")).Add(" from Basic Facilities.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).Add(baseInfo.permanentBudgetFromAdvancedFacilities, TextStyle.Get("GreenTextNoNewline")).Add(" from Permanent Advancements.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).Add(BaselineMaxBudget, TextStyle.Get("GreenTextNoNewline")).Add(" Baseline.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.Open(TextStyle.Get("Newline_NoLabel_Lead")).Add(baseInfo.GetPerSecondBudget, TextStyle.Get("GreenTextNoNewline")).Add(" Total Budget.").Close(TextStyle.Get("Newline_NoLabel_Lead"));
            Buffer.EndStatement(TextStyle.Get("Newline"));
        }
        //AddStrength_MoreReadable(baseInfo.Strength.Display / 1000, true)
        //baseInfo.GetMaxStrength / 1000 
    }
}