using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PerkFixesAndRebalance
{
    /// <summary>
    /// Perk data edits. Perks are re-initialized right after their owner's InitializeAll, which runs on every new or loaded
    /// campaign, so the edited bonuses, roles, descriptions and captain troop-usage masks are what the game and the perk UI
    /// both see.
    /// </summary>
    internal static class PerkChanges
    {
        private const int VanillaVandalPercent = 25;
        private const int VanillaShatteringPercent = 50;

        private static DefaultPerks _vanillaPerks;
        private static object _navalPerks;
        private static DefaultSkillEffects _skillEffects;

        // Set at load (null when the change is off) so combat and wage code match the descriptions if MCM is toggled mid-game.
        public static PerkObject RestrictedVandal { get; private set; }
        public static PerkObject RestrictedPickedShots { get; private set; }
        public static PerkObject SeaOnlyShatteringBlow { get; private set; }
        public static PerkObject SeaOnlyShatteringVolley { get; private set; }
        public static bool ShieldMovementSpeedFix { get; private set; }
        public static bool FormFittingArmorOnce { get; private set; }
        public static bool WrappedHandlesAllTroops { get; private set; }
        public static bool StrongGripAllTroops { get; private set; }
        public static bool CleanThrustAllTroops { get; private set; }
        public static bool CounterweightAllTroops { get; private set; }
        public static bool FlexibleFighterCavalry { get; private set; }
        public static bool LeaderOrRoleHero { get; private set; }
        public static bool TacticsBattleEffects { get; private set; }
        public static bool TacticalMasteryPartyLeader { get; private set; }
        public static bool ArmyCommanderPerksWhenAlone { get; private set; }
        public static bool ArtisanCommunityQuartermaster { get; private set; }
        public static bool RapidDevelopmentQuartermaster { get; private set; }
        public static bool MasterworkEngineerSkill { get; private set; }
        public static PerkObject Commodore { get; private set; }
        public static PerkObject EnemyOfTheWood { get; private set; }
        public static PerkObject PortAuthority { get; private set; }
        public static PerkObject BlessingsOfTheSea { get; private set; }
        public static PerkObject Stormrider { get; private set; }
        public static PerkObject MasterAndCommander { get; private set; }
        public static PerkObject FirstMateStreamlinedOperations { get; private set; }
        public static PerkObject FirstMateSpecialArrows { get; private set; }
        public static PerkObject FirstMateSmoothOperator { get; private set; }
        public static PerkObject FirstMateAccuracyTraining { get; private set; }
        public static PerkObject ArrCaptainXp { get; private set; }
        public static PerkObject PirateHunterCaptainXp { get; private set; }

        public static void ApplyVanilla(DefaultPerks perks)
        {
            _vanillaPerks = perks;
            PerkFixesAndRebalanceSettings s = Settings.Current;
            CrossbowTroops.Clear();

            RestrictedVandal = null;
            if (s.VandalTwoHandedOnly || s.VandalArmorPenetration != VanillaVandalPercent)
            {
                PerkObject vandal = Edit(perks, "_twoHandedVandal", primaryBonus: s.VandalArmorPenetration / 100f,
                    primaryDescription: s.VandalTwoHandedOnly ? "{=PFx_Vandal}{VALUE}% armor penetration with two handed weapons." : null);
                RestrictedVandal = s.VandalTwoHandedOnly ? vandal : null;
            }

            bool clarify = s.ClarifyPerkDescriptions;

            if (clarify)
            {
                Edit(perks, "_oneHandedChinkInTheArmor",
                    primaryDescription: "{=PFx_Chink}{VALUE}% armor penetration with one handed weapons.");
                Edit(perks, "_bowHorseMaster",
                    secondaryDescription: "{=PFx_HorseMaster}{VALUE} Bow skill to cavalry and horse archers in your formation, mounted or on foot.");
                Edit(perks, "_crossbowFletcher",
                    primaryDescription: "{=PFx_FletcherA}{VALUE} bolts per crossbow quiver.",
                    secondaryDescription: "{=PFx_FletcherB}{VALUE} bolts per crossbow quiver to troops in your party.");
                Edit(perks, "_bowDeepQuivers",
                    primaryDescription: "{=PFx_DeepQuiversA}{VALUE} extra arrows per bow quiver.",
                    secondaryDescription: "{=PFx_DeepQuiversB}{VALUE} extra arrow per bow quiver to troops in your party.");
                foreach ((string field, string text) in CaptainTexts)
                {
                    EditCaptain(perks, field, text);
                }
            }

            WrappedHandlesAllTroops = s.WrappedHandlesAllTroops;
            StrongGripAllTroops = s.StrongGripAllTroops;
            CleanThrustAllTroops = s.CleanThrustAllTroops;
            CounterweightAllTroops = s.CounterweightAllTroops;
            FlexibleFighterCavalry = s.FlexibleFighterCavalry;
            if (FlexibleFighterCavalry)
            {
                EditCaptain(perks, "_throwingFlexibleFighter", "{=PFx_FlexibleFighterCav}{VALUE} Bow, Crossbow and Throwing skill to infantry and cavalry, and {VALUE} One Handed, Two Handed and Polearm skill to archers and horse archers in your formation.");
            }
            else if (clarify)
            {
                EditCaptain(perks, "_throwingFlexibleFighter", "{=PFx_FlexibleFighter}{VALUE} Bow, Crossbow and Throwing skill to infantry, and {VALUE} One Handed, Two Handed and Polearm skill to archers and horse archers in your formation.");
            }
            const string noHorse = " skill to infantry and archers in your formation.";
            if (WrappedHandlesAllTroops)
                EditCaptain(perks, "_oneHandedWrappedHandles", "{=PFx_WrappedHandlesAll}{VALUE} One Handed skill to troops in your formation.");
            else if (clarify)
                EditCaptain(perks, "_oneHandedWrappedHandles", "{=PFx_WrappedHandles}{VALUE} One Handed" + noHorse);
            if (StrongGripAllTroops)
                EditCaptain(perks, "_twoHandedStrongGrip", "{=PFx_StrongGripAll}{VALUE} Two Handed skill to troops in your formation.");
            else if (clarify)
                EditCaptain(perks, "_twoHandedStrongGrip", "{=PFx_StrongGrip}{VALUE} Two Handed" + noHorse);
            if (CleanThrustAllTroops)
                EditCaptain(perks, "_polearmCleanThrust", "{=PFx_CleanThrustAll}{VALUE} Polearm skill to troops in your formation.");
            else if (clarify)
                EditCaptain(perks, "_polearmCleanThrust", "{=PFx_CleanThrust}{VALUE} Polearm" + noHorse);
            if (!CounterweightAllTroops && clarify)
                EditCaptain(perks, "_polearmCounterweight", "{=PFx_Counterweight}{VALUE} Polearm" + noHorse);

            ApplyCaptainTroopUsageMasks(perks, s);

            ShieldMovementSpeedFix = s.ShieldMovementSpeedFix;
            FormFittingArmorOnce = s.FormFittingArmorOnce;
            if (clarify)
            {
                ApplyMovementSpeedDescriptions(perks, ShieldMovementSpeedFix, FormFittingArmorOnce);
            }

            LeaderOrRoleHero = s.LeaderOrRoleHero;
            TacticsBattleEffects = s.TacticsBattleEffects;
            TacticalMasteryPartyLeader = s.TacticalMasteryPartyLeader;
            ArmyCommanderPerksWhenAlone = s.ArmyCommanderPerksWhenAlone;
            // Vanilla-accurate autoresolve wording under Clarify; experimental battle-mirror notes override when that option is on
            // (even if Clarify is off, so the MCM change is visible in tooltips).
            if (clarify)
            {
                ApplyTacticsAutoresolveDescriptions(perks);
            }
            if (TacticsBattleEffects)
            {
                ApplyTacticsBattleDescriptions(perks);
            }

            if (TacticalMasteryPartyLeader)
            {
                const string sea = "\nEither effect is half as effective at sea.";
                string masterySim = "{=PFx4_MasteryPL}{VALUE}% damage for every Tactics point above 200 when auto-resolving.\nHalf as effective at sea.";
                string masteryBat = "{=PFx4_MasteryPLBat}{VALUE}% damage for every Tactics point above 200 when auto-resolving.\n"
                    + "+0.05% more damage dealt and less damage taken per point above 200 in land battles you fight."
                    + sea;
                Edit(perks, "_tacticsTacticalMastery",
                    primaryRole: PartyRole.PartyLeader,
                    primaryDescription: TacticsBattleEffects ? masteryBat : (clarify ? masterySim : null));
            }

            ArtisanCommunityQuartermaster = s.ArtisanCommunityQuartermaster;
            RapidDevelopmentQuartermaster = s.RapidDevelopmentQuartermaster;
            MasterworkEngineerSkill = s.MasterworkEngineerSkill;

            if (s.FortitudeTonicSurgeon)
            {
                Edit(perks, "_medicineFortitudeTonic", primaryRole: PartyRole.Surgeon);
            }

            float perPoint = s.MinisterOfHealthHitpointsPerSkillPoint;
            if (s.MinisterOfHealthSurgeon || Math.Abs(perPoint - 1f) > 0.001f)
            {
                // Own variable: the {VALUE} formatter rounds to one decimal, which would show 0.675 as 0.7.
                SetMinisterHitpoints(Edit(perks, "_medicineMinisterOfHealth",
                    primaryRole: s.MinisterOfHealthSurgeon ? PartyRole.Surgeon : (PartyRole?)null, primaryBonus: perPoint,
                    primaryDescription: "{=PFx_Minister}+{HP_PER_POINT} hit points to troops for every point of Medicine above 250."), perPoint);
            }
            else if (clarify)
            {
                // Vanilla reads the party leader, but labels Personal.
                // A re-apply keeps the text set above, so its variable needs a value again.
                SetMinisterHitpoints(Edit(perks, "_medicineMinisterOfHealth", primaryRole: PartyRole.PartyLeader), perPoint);
            }

            if (s.SledgesSurgeon)
            {
                Edit(perks, "_medicineSledges", secondaryRole: PartyRole.Surgeon);
            }

            if (clarify)
            {
                ApplyVanillaRoleLabelFixes(perks);
                Edit(perks, "_rogueryWhiteLies",
                    secondaryDescription: "{=PFx_WhiteLiesGov}{VALUE}% chance per day for +1 relation with a random notable in the governed settlement.");
                Edit(perks, "_rogueryScarface",
                    secondaryDescription: "{=PFx_ScarfaceGov}{VALUE}% chance per day for +1 relation with a random gang leader in the governed settlement.");
            }

            RestrictedPickedShots = s.PickedShotsCrossbowOnly
                ? Edit(perks, "_crossbowPickedShots",
                    primaryDescription: "{=PFx_PickedShotsA}{VALUE}% wages of tier 4+ crossbow troops.",
                    secondaryDescription: "{=PFx_PickedShotsB}{VALUE} hit points to crossbow troops in your party.")
                : null;
        }

        /// <summary>
        /// Role headers that do not match how the game reads the perk. Label-only; application sites use LeaderHero /
        /// clan.Leader / captain GetPerkValue directly.
        /// </summary>
        private static void ApplyVanillaRoleLabelFixes(DefaultPerks perks)
        {
            Edit(perks, "_rogueryRogueExtraordinaire", primaryRole: PartyRole.PartyLeader);
            Edit(perks, "_charmImmortalCharm", primaryRole: PartyRole.ClanLeader);
            Edit(perks, "_rogueryDeepPockets", secondaryRole: PartyRole.PartyLeader);
            Edit(perks, "_leadershipWePledgeOurSwords", primaryRole: PartyRole.ClanLeader);
            // Disorganized-morale check uses LeaderHero, not any party member.
            Edit(perks, "_tacticsImproviser", primaryRole: PartyRole.PartyLeader);

            // Battle/siege rewards and party trade: Personal label, but code reads the party leader.
            Edit(perks, "_charmWarlord", primaryRole: PartyRole.PartyLeader);
            Edit(perks, "_leadershipFamousCommander",
                primaryRole: PartyRole.PartyLeader, secondaryRole: PartyRole.PartyLeader);
            Edit(perks, "_tacticsBesieged", secondaryRole: PartyRole.PartyLeader);
            Edit(perks, "_tradeGranaryAccountant", primaryRole: PartyRole.PartyLeader);
            Edit(perks, "_tradeTradeyardForeman", primaryRole: PartyRole.PartyLeader);
        }

        public static void ApplyNaval(object perks)
        {
            _navalPerks = perks;
            PerkFixesAndRebalanceSettings s = Settings.Current;

            SeaOnlyShatteringBlow = null;
            SeaOnlyShatteringVolley = null;
            if (s.ShatteringNavalOnly || s.ShatteringArmorPenetration != VanillaShatteringPercent)
            {
                bool naval = s.ShatteringNavalOnly;
                float bonus = s.ShatteringArmorPenetration / 100f;
                PerkObject blow = Edit(perks, "_shatteringBlow", primaryBonus: bonus, secondaryBonus: bonus,
                    primaryDescription: naval ? "{=PFx_ShatterBlowA}{VALUE}% armor penetration for melee weapons in naval battles." : null,
                    secondaryDescription: naval ? "{=PFx_ShatterBlowB}{VALUE}% armor penetration for melee weapons wielded by crew in naval battles." : null);
                PerkObject volley = Edit(perks, "_shatteringVolley", primaryBonus: bonus, secondaryBonus: bonus,
                    primaryDescription: naval ? "{=PFx_ShatterVolleyA}{VALUE}% armor penetration for ranged weapons in naval battles." : null,
                    secondaryDescription: naval ? "{=PFx_ShatterVolleyB}{VALUE}% armor penetration for ranged weapons wielded by crew in naval battles." : null);
                SeaOnlyShatteringBlow = naval ? blow : null;
                SeaOnlyShatteringVolley = naval ? volley : null;
            }

            ArmyCommanderPerksWhenAlone = s.ArmyCommanderPerksWhenAlone;
            Commodore = Get(perks, "_commodore");
            EnemyOfTheWood = Get(perks, "_enemyOfTheWood");

            bool clarify = s.ClarifyPerkDescriptions;
            if (clarify)
            {
                Edit(perks, "_bruteForce",
                    primaryDescription: "{=PFx_BruteForce}{VALUE}% kicking and bashing damage, on land and at sea.");
                Edit(perks, "_navalBombardment",
                    primaryDescription: "{=PFx_NavalBombardment}Shipboard ballistas fire during sieges. Not implemented in War Sails: has no effect.");
                Edit(perks, "_commodore",
                    primaryDescription: ArmyCommanderPerksWhenAlone
                        ? "{=PFx_CommodoreAlone}Your flagship figurehead bonus applies to all of your ships' crews. In an army you lead, each party gets the same from its own flagship."
                        : "{=PFx_Commodore}In an army you lead, each party's flagship figurehead bonus applies to that party's troops on all of its ships.");
            }

            FirstMateStreamlinedOperations = s.StreamlinedOperationsFirstMate
                ? Edit(perks, "_streamlinedOperations", primaryDescription: "{=PFx_StreamlinedOps}{VALUE}% ballista reload speed.")
                : null;
            FirstMateSpecialArrows = s.SpecialArrowsFirstMate ? Get(perks, "_specialArrows") : null;
            FirstMateSmoothOperator = s.SmoothOperatorFirstMate ? Get(perks, "_smoothOperator") : null;
            FirstMateAccuracyTraining = s.AccuracyTrainingFirstMate ? Get(perks, "_accuracyTraining") : null;

            ArrCaptainXp = null;
            PirateHunterCaptainXp = null;
            if (s.ArrPirateHunterCaptainXp)
            {
                EditCaptain(perks, "_arr",
                    "{=PFx_Arr}{VALUE}% xp gain after each naval battle for mariner troops under character's command.");
                EditCaptain(perks, "_pirateHunter",
                    "{=PFx_PirateHunter}{VALUE}% xp gain after each naval battle for regular troops under character's command.");
                ArrCaptainXp = Get(perks, "_arr");
                PirateHunterCaptainXp = Get(perks, "_pirateHunter");
            }

            PortAuthority = Get(perks, "_portAuthority");
            BlessingsOfTheSea = Get(perks, "_blessingsOfTheSea");
            Stormrider = Get(perks, "_stormrider");
            MasterAndCommander = Get(perks, "_masterAndCommander");

            if (clarify)
            {
                const string shipLimit = "{=PFx_ShipLimit}{VALUE} ship to command limit in naval battles.";
                Edit(perks, "_portAuthority", primaryDescription: shipLimit);
                Edit(perks, "_blessingsOfTheSea", primaryDescription: shipLimit);
                Edit(perks, "_merchantFleet", primaryDescription: shipLimit);
                Edit(perks, "_stormrider", secondaryDescription: shipLimit);
                Edit(perks, "_masterAndCommander", secondaryDescription: shipLimit);
                foreach ((string field, string text) in NavalCaptainTexts)
                {
                    EditCaptain(perks, field, text);
                }

                ApplyNavalRoleLabelFixes(perks);
            }

            ApplyNavalCaptainTroopUsageMasks(perks);

            if (s.PortAuthorityFirstMate) Edit(perks, "_portAuthority", primaryRole: PartyRole.FirstMate);
            if (s.BlessingsOfTheSeaFirstMate) Edit(perks, "_blessingsOfTheSea", primaryRole: PartyRole.FirstMate);
            if (s.MerchantFleetFirstMate) Edit(perks, "_merchantFleet", primaryRole: PartyRole.FirstMate);
            if (s.GildedPurseFirstMate) Edit(perks, "_gildedPurse", primaryRole: PartyRole.FirstMate);
            if (s.PopularCaptainFirstMate) Edit(perks, "_popularCaptain", secondaryRole: PartyRole.FirstMate);
            if (s.ShipwrightsHandFirstMate) Edit(perks, "_shipwrightsHand", primaryRole: PartyRole.FirstMate);

            if (s.UnflinchingNavigator) Edit(perks, "_unflinching", primaryRole: PartyRole.Navigator);
            if (s.ShoreMasterNavigator) Edit(perks, "_shoreMaster", primaryRole: PartyRole.Navigator);

            if (s.StormriderNavigator) Edit(perks, "_stormrider", secondaryRole: PartyRole.Navigator);
            if (s.MasterAndCommanderNavigator) Edit(perks, "_masterAndCommander", secondaryRole: PartyRole.Navigator);
            if (s.FleetCommanderNavigator)
            {
                Edit(perks, "_fleetCommander", primaryRole: PartyRole.Navigator, secondaryRole: PartyRole.Navigator);
            }
        }

        /// <summary>
        /// Clarify-only auto-resolve texts. These party-leader sim effects are land-only outliers vs most combat perks;
        /// Tactical Mastery is the further exception that also runs at sea at half strength.
        /// </summary>
        private static void ApplyTacticsAutoresolveDescriptions(DefaultPerks perks)
        {
            Edit(perks, "_tacticsTightFormations",
                primaryDescription: "{=PFx4_TightSim}{VALUE}% damage by your infantry against cavalry when auto-resolving land battles.");
            Edit(perks, "_tacticsLooseFormations",
                primaryDescription: "{=PFx4_LooseSim}{VALUE}% damage taken by your infantry from ranged troops when auto-resolving land battles.");
            Edit(perks, "_tacticsExtendedSkirmish",
                primaryDescription: "{=PFx4_ExtSkirmSim}{VALUE}% damage in snowy and forest terrain when auto-resolving land battles.");
            Edit(perks, "_tacticsDecisiveBattle",
                primaryDescription: "{=PFx4_DecBattleSim}{VALUE}% damage in plains, steppes and deserts when auto-resolving land battles.");
            Edit(perks, "_tacticsLawKeeper",
                primaryDescription: "{=PFx4_LawKeeperSim}{VALUE}% damage against bandits when auto-resolving land battles.");
            Edit(perks, "_tacticsCoaching",
                primaryDescription: "{=PFx4_CoachingSim}{VALUE}% damage when auto-resolving land battles.");
            Edit(perks, "_tacticsEliteReserves",
                primaryDescription: "{=PFx4_EliteSim}{VALUE}% damage taken by tier 3+ troops when auto-resolving land battles.");
            Edit(perks, "_tacticsEncirclement",
                primaryDescription: "{=PFx4_EncircleSim}{VALUE}% damage when auto-resolving land battles while you outnumber the enemy.");
            Edit(perks, "_tacticsCounteroffensive",
                primaryDescription: "{=PFx4_CounterOffASim}{VALUE}% damage when auto-resolving as the attacker in a land field battle.",
                secondaryDescription: "{=PFx4_CounterOffBSim}{VALUE}% damage when auto-resolving land battles while outnumbered.");
            Edit(perks, "_scoutingVanguard",
                primaryDescription: "{=PFx4_VanguardASim}{VALUE}% damage when auto-resolving land battles as the attacker.",
                secondaryDescription: "{=PFx4_VanguardBSim}{VALUE}% damage when auto-resolving a land sally out.");
            Edit(perks, "_scoutingRearguard",
                secondaryDescription: "{=PFx4_RearguardSim}{VALUE}% damage when auto-resolving land battles while defending your siege camp.");
            Edit(perks, "_leadershipTrustedCommander",
                secondaryDescription: "{=PFx4_TrustedSim}{VALUE}% experience for troops when auto-resolving land battles.");

            Edit(perks, "_tacticsPreBattleManeuvers",
                secondaryDescription: "{=PFx4_PreBattleSim}{VALUE}% battle advantage for every 100 Tactics your side's commander has over the enemy commander when auto-resolving land battles.");
            Edit(perks, "_scoutingPatrols",
                secondaryDescription: "{=PFx4_PatrolsSim}{VALUE}% battle advantage against bandits when auto-resolving land battles. Uses your side's commander.");

            Edit(perks, "_tacticsBesieged",
                primaryDescription: "{=PFx4_BesiegedSim}{VALUE}% damage when auto-resolving as the attacker in a siege. Needs the perk on any hero in this party.");
            Edit(perks, "_tacticsOnTheMarch",
                primaryDescription: "{=PFx4_OnTheMarchSim}{VALUE}% fortification bonus to the enemy when auto-resolving a land siege.");
            if (!TacticalMasteryPartyLeader)
            {
                Edit(perks, "_tacticsTacticalMastery",
                    primaryDescription: "{=PFx4_MasterySim}{VALUE}% damage for every Tactics point above 200 when auto-resolving. Only the army leader's own party.\nHalf as effective at sea.");
            }
        }

        /// <summary>
        /// Experimental: auto-resolve line, then a second line that mirrors the same effect for battles you fight.
        /// </summary>
        private static void ApplyTacticsBattleDescriptions(DefaultPerks perks)
        {
            Edit(perks, "_tacticsTightFormations",
                primaryDescription: "{=PFx4_TightBat}{VALUE}% damage by your infantry against cavalry when auto-resolving land battles.\n+5% damage by your infantry against cavalry in land battles you fight.");
            Edit(perks, "_tacticsLooseFormations",
                primaryDescription: "{=PFx4_LooseBat}{VALUE}% damage taken by your infantry from ranged troops when auto-resolving land battles.\n-5% damage taken by your infantry from ranged troops in land battles you fight.");
            Edit(perks, "_tacticsExtendedSkirmish",
                primaryDescription: "{=PFx4_ExtSkirmBat}{VALUE}% damage in snowy and forest terrain when auto-resolving land battles.\n+5% damage in snowy and forest terrain in land battles you fight.");
            Edit(perks, "_tacticsDecisiveBattle",
                primaryDescription: "{=PFx4_DecBattleBat}{VALUE}% damage in plains, steppes and deserts when auto-resolving land battles.\n+2.5% damage in plains, steppes and deserts in land battles you fight.");
            Edit(perks, "_tacticsLawKeeper",
                primaryDescription: "{=PFx4_LawKeeperBat}{VALUE}% damage against bandits when auto-resolving land battles.\n+5% damage against bandits in land battles you fight.");
            Edit(perks, "_tacticsCoaching",
                primaryDescription: "{=PFx4_CoachingBat}{VALUE}% damage when auto-resolving land battles.\n+1% damage in land battles you fight.");
            Edit(perks, "_tacticsEliteReserves",
                primaryDescription: "{=PFx4_EliteBat}{VALUE}% damage taken by tier 3+ troops when auto-resolving land battles.\n-4% damage taken by tier 3+ troops in land battles you fight.");
            Edit(perks, "_tacticsEncirclement",
                primaryDescription: "{=PFx4_EncircleBat}{VALUE}% damage when auto-resolving land battles while you outnumber the enemy.\n+2.5% damage while you outnumber the enemy in land battles you fight.");
            Edit(perks, "_tacticsCounteroffensive",
                primaryDescription: "{=PFx4_CounterOffABat}{VALUE}% damage when auto-resolving as the attacker in a land field battle.\n+2% damage as the attacker in land field battles you fight.",
                secondaryDescription: "{=PFx4_CounterOffBBat}{VALUE}% damage when auto-resolving land battles while outnumbered.\n+2% damage while outnumbered in land battles you fight.");
            Edit(perks, "_scoutingVanguard",
                primaryDescription: "{=PFx4_VanguardABat}{VALUE}% damage when auto-resolving land battles as the attacker.\n+2.5% damage as the attacker in land battles you fight.",
                secondaryDescription: "{=PFx4_VanguardBBat}{VALUE}% damage when auto-resolving a land sally out.\n+5% damage in land sally outs you fight.");
            Edit(perks, "_scoutingRearguard",
                secondaryDescription: "{=PFx4_RearguardBat}{VALUE}% damage when auto-resolving land battles while defending your siege camp.\n+5% damage while defending your siege camp in land battles you fight.");
            Edit(perks, "_leadershipTrustedCommander",
                secondaryDescription: "{=PFx4_TrustedBat}{VALUE}% experience for troops when auto-resolving land battles.\n+4% experience for troops in land battles you fight.");

            Edit(perks, "_tacticsPreBattleManeuvers",
                secondaryDescription: "{=PFx4_PreBattleBat}{VALUE}% damage for every 100 Tactics your side's commander has over the enemy commander when auto-resolving land battles.\nSame damage bonus in land battles you fight.");
            Edit(perks, "_scoutingPatrols",
                secondaryDescription: "{=PFx4_PatrolsBat}{VALUE}% battle advantage against bandits when auto-resolving land battles.\n+5% damage against bandits in land battles you fight. Uses your side's commander.");

            Edit(perks, "_tacticsBesieged",
                primaryDescription: "{=PFx4_BesiegedBat}{VALUE}% damage when auto-resolving as the attacker in a siege.\n+5% damage as the attacker in sieges you fight. Needs the perk on any hero in this party.");
            Edit(perks, "_tacticsOnTheMarch",
                primaryDescription: "{=PFx4_OnTheMarchBat}{VALUE}% fortification bonus to the enemy when auto-resolving a land siege.\n+2% damage when attacking in sieges you fight.");
            if (!TacticalMasteryPartyLeader)
            {
                Edit(perks, "_tacticsTacticalMastery",
                    primaryDescription: "{=PFx4_MasteryBat}{VALUE}% damage for every Tactics point above 200 when auto-resolving. Only the army leader's own party.\n"
                        + "+0.05% more damage dealt and less damage taken per point above 200 in land battles you fight.\n"
                        + "Either effect is half as effective at sea.");
            }
        }

        public static void ApplySkillEffects(DefaultSkillEffects effects)
        {
            _skillEffects = effects;
            PerkFixesAndRebalanceSettings s = Settings.Current;
            if (!s.ClarifyPerkDescriptions && !s.TacticsBattleEffects)
            {
                return;
            }

            SkillEffect advantage = Traverse.Create(effects).Field("_effectTacticsAdvantage").GetValue<SkillEffect>();
            SkillEffect sacrifice = Traverse.Create(effects).Field("_effectTacticsTroopSacrificeReduction").GetValue<SkillEffect>();
            SkillEffect morale = Traverse.Create(effects).Field("_effectLeadershipMoraleBonus").GetValue<SkillEffect>();
            SkillEffect garrison = Traverse.Create(effects).Field("_effectLeadershipGarrisonSizeBonus").GetValue<SkillEffect>();

            if (sacrifice != null && s.ClarifyPerkDescriptions)
            {
                // Still Personal: fleeing uses the player character, not party leader role lookup.
                EditSkillEffect(sacrifice, "{=PFx5_TacticsFlee}Fewer troops lost when fleeing: +{a0}%");
            }

            // Fight damage line is a separate bullet in TacticsSkillTooltipPatch (also Party Leader).
            if (advantage != null && (s.ClarifyPerkDescriptions || s.TacticsBattleEffects))
            {
                EditSkillEffect(advantage, "{=PFx5_TacticsAdv}Battle advantage when auto-resolving land battles: +{a0}%",
                    PartyRole.PartyLeader);
            }

            if (s.ClarifyPerkDescriptions)
            {
                if (morale != null)
                {
                    EditSkillEffect(morale, "{=PFx5_LeadMorale}Party morale: +{a0}", PartyRole.PartyLeader);
                }

                // Code reads the clan leader's Leadership, not the party leader.
                if (garrison != null)
                {
                    EditSkillEffect(garrison, "{=PFx5_LeadGarrison}Garrison size: +{a0}", PartyRole.ClanLeader);
                }
            }
        }

        private static void EditSkillEffect(SkillEffect effect, string description, PartyRole? role = null)
        {
            effect.Initialize(
                new TextObject(description),
                effect.EffectedSkill,
                role ?? effect.Role,
                effect.Bonus,
                effect.IncrementType,
                effect.BaseValue,
                effect.LimitMin,
                effect.LimitMax);
        }

        /// <summary>
        /// Fills {HP_PER_POINT} in the Minister of Health text. Set on both objects: with no secondary line,
        /// PerkObject.Initialize gives Description a copy of PrimaryDescription.
        /// </summary>
        private static void SetMinisterHitpoints(PerkObject perk, float perPoint)
        {
            if (perk == null)
            {
                return;
            }

            string value = perPoint.ToString("0.###");
            perk.PrimaryDescription?.SetTextVariable("HP_PER_POINT", value);
            perk.Description?.SetTextVariable("HP_PER_POINT", value);
        }

        /// <summary>Re-apply after MCM is ready; InitializeAll can run before settings load.</summary>
        internal static void ReapplyAll()
        {
            if (_vanillaPerks != null)
            {
                ApplyVanilla(_vanillaPerks);
            }

            if (_navalPerks != null)
            {
                ApplyNaval(_navalPerks);
            }

            if (_skillEffects != null)
            {
                ApplySkillEffects(_skillEffects);
            }
        }

        /// <summary>
        /// Captain movement speed raises the troop's own speed multiplier, which a mounted troop doesn't use. Without the
        /// shield fix, the first seven are only read while the troop has no shield in its off hand; without the Form Fitting
        /// fix, Form Fitting Armor is read a second time for tier 3+ infantry. Sprint checks the infantry troop class.
        /// </summary>
        private static void ApplyMovementSpeedDescriptions(DefaultPerks perks, bool shieldFix, bool formFittingOnce)
        {
            // Whole sentences per variant: a localized string can't be spliced from English fragments.
            Edit(perks, "_athleticsMorningExercise", secondaryDescription: shieldFix
                ? "{=PFx_MorningExercise}{VALUE}% combat movement speed to troops on foot in your formation."
                : "{=PFx_MorningExerciseNoShield}{VALUE}% combat movement speed to troops on foot in your formation not holding a shield.");
            Edit(perks, "_oneHandedShieldBearer", secondaryDescription: shieldFix
                ? "{=PFx_ShieldBearer}{VALUE}% movement speed to troops on foot in your formation."
                : "{=PFx_ShieldBearerNoShield}{VALUE}% movement speed to troops on foot in your formation not holding a shield.");
            Edit(perks, "_oneHandedFleetOfFoot", secondaryDescription: shieldFix
                ? "{=PFx_FleetOfFoot}{VALUE}% movement speed to troops on foot in your formation."
                : "{=PFx_FleetOfFootNoShield}{VALUE}% movement speed to troops on foot in your formation not holding a shield.");
            Edit(perks, "_polearmFootwork", secondaryDescription: shieldFix
                ? "{=PFx_Footwork}{VALUE}% movement speed to troops on foot in your formation."
                : "{=PFx_FootworkNoShield}{VALUE}% movement speed to troops on foot in your formation not holding a shield.");
            Edit(perks, "_twoHandedRecklessCharge", secondaryDescription: shieldFix
                ? "{=PFx_RecklessCharge}{VALUE}% damage with any attack and movement speed to troops on foot in your formation."
                : "{=PFx_RecklessChargeShield}{VALUE}% damage with any attack and movement speed to troops on foot in your formation. No movement speed while holding a shield.");
            if (!shieldFix)
            {
                Edit(perks, "_athleticsSprint", secondaryDescription: "{=PFx_Sprint}{VALUE}% combat movement speed to infantry in your formation not holding a shield.");
            }

            Edit(perks, "_athleticsFormFittingArmor", secondaryDescription: formFittingOnce
                ? "{=PFx_FormFittingOnce}{VALUE}% combat movement speed to tier 3+ troops on foot in your formation."
                : shieldFix
                    ? "{=PFx_FormFittingShield}{VALUE}% combat movement speed to tier 3+ troops on foot in your formation, doubled for infantry."
                    : "{=PFx_FormFitting}{VALUE}% combat movement speed to tier 3+ infantry in your formation, and again to tier 3+ troops on foot not holding a shield.");

            Edit(perks, "_bowNockingPoint", secondaryDescription: "{=PFx_NockingPoint}{VALUE}% movement speed to troops on foot in your formation wielding a bow.");
            Edit(perks, "_crossbowLooseAndMove", secondaryDescription: "{=PFx_LooseAndMove}{VALUE}% movement speed to troops on foot in your formation wielding a ranged weapon.");
            Edit(perks, "_tacticsDecisiveBattle", secondaryDescription: "{=PFx_DecisiveBattle}{VALUE}% movement speed to troops on foot in your formation in plains, steppes and deserts.");
            Edit(perks, "_tacticsExtendedSkirmish", secondaryDescription: "{=PFx_ExtendedSkirmish}{VALUE}% movement speed to troops on foot in your formation in snowy and forest terrains.");
            Edit(perks, "_tacticsSmallUnitTactics", secondaryDescription: "{=PFx_SmallUnitTactics}{VALUE}% movement speed to troops on foot in your formation when it has 15 or fewer soldiers.");
        }

        // Captain texts that match the code. Wording: "troops" is any unit; infantry, archers, cavalry and horse archers
        // are troop classes from the loadout; "on foot" and "mounted" are the troop's state at that moment.
        private static readonly (string Field, string Text)[] CaptainTexts =
        {
            ("_oneHandedBasher", "{=PFx_Basher}{VALUE}% melee damage taken by troops in your formation while it is in shield wall."),
            ("_oneHandedCavalry", "{=PFx_Cavalry1H}{VALUE}% melee damage by mounted troops in your formation."),
            ("_oneHandedSteelCoreShields", "{=PFx_SteelCore}{VALUE}% damage to shields of troops on foot in your formation."),
            ("_oneHandedDeadlyPurpose", "{=PFx_DeadlyPurpose}{VALUE}% melee damage by troops on foot in your formation."),
            ("_twoHandedWoodChopper", "{=PFx_WoodChopper}{VALUE}% damage against shields with two handed weapons by troops in your formation."),
            ("_twoHandedHeadBasher", "{=PFx_HeadBasher}{VALUE}% damage with any attack by troops on foot in your formation."),
            ("_twoHandedOnTheEdge", "{=PFx_OnTheEdge}{VALUE}% melee swing speed to troops on foot in your formation."),
            ("_twoHandedShieldBreaker", "{=PFx_ShieldBreaker2H}{VALUE}% damage against shields with two handed weapons by troops in your formation."),
            ("_twoHandedBeastSlayer", "{=PFx_BeastSlayer}{VALUE}% damage to mounts with two handed weapons by troops in your formation."),
            ("_twoHandedBladeMaster", "{=PFx_BladeMaster}{VALUE}% melee attack speed to troops on foot in your formation."),
            ("_polearmCavalry", "{=PFx_CavalryPolearm}{VALUE}% damage with any attack, including horse charges, by mounted troops in your formation."),
            ("_polearmPikeman", "{=PFx_Pikeman}{VALUE}% damage with any attack by troops on foot in your formation."),
            ("_polearmBraced", "{=PFx_BracedPolearm}{VALUE}% damage with any attack against mounted troops and their mounts by troops on foot in your formation."),
            ("_polearmSwiftSwing", "{=PFx_SwiftSwing}{VALUE}% melee swing speed to troops on foot in your formation."),
            ("_polearmSteedKiller", "{=PFx_SteedKiller}{VALUE}% damage to mounts with polearms by troops on foot in your formation."),
            ("_polearmUnstoppableForce", "{=PFx_UnstoppableForce}{VALUE}% damage bonus from speed with polearms to mounted troops in your formation."),
            ("_polearmSharpenTheTip", "{=PFx_SharpenTheTip}{VALUE}% thrust damage with melee weapons by troops on foot in your formation."),
            ("_bowBowControl", "{=PFx_BowControl}{VALUE}% damage with bows by troops in your formation. Doesn't apply to shields."),
            ("_bowQuickAdjustments", "{=PFx_QuickAdjustments}{VALUE}% accuracy penalty with bows to troops in your formation."),
            ("_bowRapidFire", "{=PFx_RapidFire}{VALUE}% bow reload speed to troops in your formation."),
            ("_bowStrongBows", "{=PFx_StrongBows}{VALUE}% damage with bows by tier 3+ troops in your formation. Doesn't apply to shields."),
            ("_bowSkirmishPhaseMaster", "{=PFx_SkirmishPhaseMaster}{VALUE}% damage taken from projectiles by archers and horse archers in your formation."),
            ("_crossbowUnhorser", "{=PFx_Unhorser}{VALUE}% crossbow damage against mounts by troops in your formation."),
            ("_crossbowSheriff", "{=PFx_Sheriff}{VALUE}% crossbow damage against troops on foot by troops in your formation."),
            ("_crossbowCounterFire", "{=PFx_CounterFire}{VALUE}% damage taken from projectiles by troops in your formation wielding a crossbow."),
            ("_crossbowHammerBolts", "{=PFx_HammerBolts}{VALUE}% damage with crossbows by troops in your formation. Doesn't apply to shields."),
            ("_throwingKnockOff", "{=PFx_KnockOff}{VALUE}% throwing damage against mounted troops and their mounts by troops in your formation."),
            ("_ridingNimbleSteed", "{=PFx_NimbleSteed}{VALUE} Riding skill to cavalry and horse archers in your formation."),
            ("_ridingHorseArcher", "{=PFx_HorseArcher}{VALUE}% ranged damage by mounted troops in your formation."),
            ("_ridingThunderousCharge", "{=PFx_ThunderousCharge}{VALUE}% battle morale penalty to enemies from weapon kills by mounted troops in your formation. Horse charge kills don't count."),
            ("_athleticsFury", "{=PFx_Fury}{VALUE}% melee weapon handling to troops on foot in your formation."),
            ("_athleticsSurgingBlow", "{=PFx_SurgingBlow}{VALUE}% damage bonus from speed to troops on foot in your formation."),
            ("_athleticsIgnorePain", "{=PFx_IgnorePain}{VALUE} armor to all equipped armor pieces of troops on foot in your formation."),
            ("_tacticsGensdarmes", "{=PFx_Gensdarmes}{VALUE}% damage with any attack, including horse charges, against troops on foot by mounted troops in your formation."),
            ("_rogueryCarver", "{=PFx_Carver}{VALUE}% one handed damage by troops in your formation."),
            ("_engineeringDreadfulSieger", "{=PFx_DreadfulBesieger}{VALUE}% damage with crossbows by troops in your formation. Doesn't apply to shields."),
        };

        private static readonly (string Field, string Text)[] NavalCaptainTexts =
        {
            ("_theSkysFury", "{=PFx_SkysFury}{VALUE}% to bow and crossbow damage dealt by crew. Doesn't apply to shields."),
            ("_specialArrows", "{=PFx_SpecialArrows}{VALUE} armor to tier 1-3 troops in your formation in naval battles."),
        };

        /// <summary>
        /// Captain troop-usage masks only affect order-of-battle captain scoring, not who gets the bonus in battle.
        /// Align each mask with the code's real conditions (and with MCM options that change those conditions).
        /// Land/Naval bits: omit both when the effect applies in either context; see <see cref="TroopUsageBits"/>.
        /// </summary>
        private static void ApplyCaptainTroopUsageMasks(DefaultPerks perks, PerkFixesAndRebalanceSettings s)
        {
            // Always: match vanilla ApplyDamage / StatCalculate conditions.
            SetCaptainMask(perks, "_crossbowCounterFire", TroopUsageFlags.CrossbowUser);
            SetCaptainMask(perks, "_crossbowSheriff", TroopUsageFlags.CrossbowUser);
            SetCaptainMask(perks, "_twoHandedWoodChopper", TroopUsageFlags.TwoHandedUser);
            SetCaptainMask(perks, "_twoHandedShieldBreaker", TroopUsageFlags.TwoHandedUser);
            SetCaptainMask(perks, "_twoHandedVandal", TroopUsageFlags.None);
            SetCaptainMask(perks, "_oneHandedBasher", TroopUsageFlags.None);
            SetCaptainMask(perks, "_tacticsSmallUnitTactics", TroopUsageFlags.OnFoot);
            SetCaptainMask(perks, "_bowNockingPoint", TroopUsageFlags.OnFoot | TroopUsageFlags.BowUser);
            SetCaptainMask(perks, "_crossbowLooseAndMove", TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged);
            SetCaptainMask(perks, "_oneHandedSteelCoreShields", TroopUsageFlags.OnFoot | TroopUsageFlags.ShieldUser);
            SetCaptainMask(perks, "_bowRapidFire", TroopUsageFlags.BowUser);
            SetCaptainMask(perks, "_ridingHorseArcher", TroopUsageFlags.Mounted | TroopUsageFlags.Ranged);
            // Sprint is IsInfantry only. Foot+Melee matches Infantry formations and excludes archers (Foot alone would include them).
            SetCaptainMask(perks, "_athleticsSprint", TroopUsageFlags.OnFoot | TroopUsageFlags.Melee);
            // Flexible Fighter: infantry, and cavalry when that option is on, get control skills. Ranged (incl. HA)
            // get vigor skills. Foot = Inf+Archer. Any also scores HA. Cavalry formations are added in
            // FlexibleFighterCavalryScorePatch when the skill option is on and this mask is still Foot.
            SetCaptainMask(perks, "_throwingFlexibleFighter",
                s.FlexibleFighterScoreHorseArchers ? TroopUsageFlags.None : TroopUsageFlags.OnFoot);

            // Skill bonuses: OnFoot when vanilla horse-in-equipment skip is on; weapon-user only when all-troops is on.
            SetCaptainMask(perks, "_oneHandedWrappedHandles",
                s.WrappedHandlesAllTroops ? TroopUsageFlags.OneHandedUser : TroopUsageFlags.OnFoot | TroopUsageFlags.OneHandedUser);
            SetCaptainMask(perks, "_twoHandedStrongGrip",
                s.StrongGripAllTroops ? TroopUsageFlags.TwoHandedUser : TroopUsageFlags.OnFoot | TroopUsageFlags.TwoHandedUser);
            SetCaptainMask(perks, "_polearmCleanThrust",
                s.CleanThrustAllTroops ? TroopUsageFlags.PolearmUser : TroopUsageFlags.OnFoot | TroopUsageFlags.PolearmUser);
            SetCaptainMask(perks, "_polearmCounterweight",
                s.CounterweightAllTroops ? TroopUsageFlags.PolearmUser : TroopUsageFlags.OnFoot | TroopUsageFlags.PolearmUser);

            // Land-only scoring: charge / mounts / land terrain. (Mounted-tagged perks already miss OnFoot sea formations.)
            SetCaptainMask(perks, "_polearmSureFooted", TroopUsageBits.Land);
            SetCaptainMask(perks, "_athleticsBraced", TroopUsageBits.Land);
            SetCaptainMask(perks, "_twoHandedBeastSlayer", TroopUsageBits.Land | TroopUsageFlags.TwoHandedUser);
            SetCaptainMask(perks, "_throwingHunter", TroopUsageBits.Land | TroopUsageFlags.ThrownUser);
            SetCaptainMask(perks, "_throwingKnockOff", TroopUsageBits.Land | TroopUsageFlags.ThrownUser);
            SetCaptainMask(perks, "_crossbowUnhorser", TroopUsageBits.Land | TroopUsageFlags.CrossbowUser);
            SetCaptainMask(perks, "_polearmBraced", TroopUsageBits.Land | TroopUsageFlags.OnFoot);
            SetCaptainMask(perks, "_polearmSteedKiller", TroopUsageBits.Land | TroopUsageFlags.OnFoot | TroopUsageFlags.PolearmUser);
            SetCaptainMask(perks, "_tacticsDecisiveBattle", TroopUsageBits.Land | TroopUsageFlags.OnFoot);
            SetCaptainMask(perks, "_tacticsExtendedSkirmish", TroopUsageBits.Land | TroopUsageFlags.OnFoot);
        }

        /// <summary>
        /// War Sails role headers that do not match formation-captain GetPerkValue reads in naval combat models.
        /// </summary>
        private static void ApplyNavalRoleLabelFixes(object perks)
        {
            // Morale-on-hit and secondary boarding mitigation both use the formation captain.
            Edit(perks, "_terrorOfTheSeas", primaryRole: PartyRole.Captain);
            // Ammo / ballista hull: labeled First Mate, read from formation captain (like vanilla Special Arrows sail).
            Edit(perks, "_wellStocked", primaryRole: PartyRole.Captain, secondaryRole: PartyRole.Captain);
            Edit(perks, "_shipwrightsInsight", primaryRole: PartyRole.Captain, secondaryRole: PartyRole.Captain);
        }

        /// <summary>
        /// Naval-only captain effects. Weapon bits preserved. All War Sails captain bonuses that only run in naval
        /// missions (or that Perk Fixes and Rebalance gates to naval) use the Naval bit.
        /// </summary>
        private static void ApplyNavalCaptainTroopUsageMasks(object perks)
        {
            SetCaptainMask(perks, "_forceful", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_enemyOfTheWood", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_rallyingCry", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_terrorOfTheSeas", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_wellStocked", TroopUsageBits.Naval | TroopUsageFlags.Ranged);
            SetCaptainMask(perks, "_shipwrightsInsight", TroopUsageBits.Naval);
            // When MCM allows shattering on land, omit Naval so they still score there.
            TroopUsageFlags shatterCtx = SeaOnlyShatteringBlow != null ? TroopUsageBits.Naval : TroopUsageFlags.None;
            SetCaptainMask(perks, "_shatteringBlow", shatterCtx | TroopUsageFlags.Melee);
            SetCaptainMask(perks, "_shatteringVolley", shatterCtx | TroopUsageFlags.Ranged);
            SetCaptainMask(perks, "_homeTurfAdvantage", TroopUsageBits.Naval | TroopUsageFlags.Melee);
            SetCaptainMask(perks, "_boardingMaster", TroopUsageBits.Naval | TroopUsageFlags.Melee);
            SetCaptainMask(perks, "_crewOfSpears", TroopUsageBits.Naval | TroopUsageFlags.ThrownUser);
            SetCaptainMask(perks, "_mightyBlows", TroopUsageBits.Naval | TroopUsageFlags.TwoHandedUser);
            SetCaptainMask(perks, "_warriorsMight", TroopUsageBits.Naval | TroopUsageFlags.ThrownUser);
            SetCaptainMask(perks, "_theSkysFury", TroopUsageBits.Naval | TroopUsageFlags.BowUser | TroopUsageFlags.CrossbowUser);
            SetCaptainMask(perks, "_oldSaltsTouch", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_windRider", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_shockAndAwe", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_windborne", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_chainToOars", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_seaborneFortress", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_specialArrows", TroopUsageBits.Naval);
            SetCaptainMask(perks, "_pirateHunter",
                ArrCaptainXp != null || PirateHunterCaptainXp != null ? TroopUsageBits.Naval : TroopUsageFlags.None);
            SetCaptainMask(perks, "_arr",
                ArrCaptainXp != null || PirateHunterCaptainXp != null ? TroopUsageBits.Naval : TroopUsageFlags.None);
        }

        private static void SetCaptainMask(object owner, string field, TroopUsageFlags mask)
        {
            PerkObject perk = Get(owner, field);
            if (perk == null)
            {
                return;
            }

            // Scoring treats either mask if != Undefined; None (0) always matches. Set every Captain side, and
            // promote non-Captain None to Undefined so PartyLeader/FirstMate sides do not inflate ratings.
            TroopUsageFlags? primary = null;
            TroopUsageFlags? secondary = null;
            if (perk.PrimaryRole == PartyRole.Captain)
            {
                primary = mask;
            }
            else if (perk.PrimaryTroopUsageMask == TroopUsageFlags.None)
            {
                primary = TroopUsageFlags.Undefined;
            }

            if (perk.SecondaryRole == PartyRole.Captain)
            {
                secondary = mask;
            }
            else if (perk.SecondaryTroopUsageMask == TroopUsageFlags.None)
            {
                secondary = TroopUsageFlags.Undefined;
            }

            if (primary != null || secondary != null)
            {
                Edit(owner, field, primaryTroopUsageMask: primary, secondaryTroopUsageMask: secondary);
            }
        }

        private static void EditCaptain(object owner, string field, string text, TroopUsageFlags? captainTroopUsageMask = null)
        {
            PerkObject perk = Get(owner, field);
            if (perk?.PrimaryRole == PartyRole.Captain)
            {
                Edit(owner, field, primaryDescription: text, primaryTroopUsageMask: captainTroopUsageMask);
            }
            else if (perk?.SecondaryRole == PartyRole.Captain)
            {
                Edit(owner, field, secondaryDescription: text, secondaryTroopUsageMask: captainTroopUsageMask);
            }
        }

        private static PerkObject Get(object owner, string field)
        {
            PerkObject perk = Traverse.Create(owner).Field(field).GetValue<PerkObject>();
            if (perk == null)
            {
                Debug.Print($"[PerkFixesAndRebalance] {owner.GetType().Name}.{field} not found");
            }

            return perk;
        }

        private static PerkObject Edit(
            object owner,
            string field,
            string primaryDescription = null,
            PartyRole? primaryRole = null,
            float? primaryBonus = null,
            string secondaryDescription = null,
            PartyRole? secondaryRole = null,
            float? secondaryBonus = null,
            TroopUsageFlags? primaryTroopUsageMask = null,
            TroopUsageFlags? secondaryTroopUsageMask = null)
        {
            try
            {
                PerkObject perk = Traverse.Create(owner).Field(field).GetValue<PerkObject>();
                if (perk == null)
                {
                    Debug.Print($"[PerkFixesAndRebalance] {owner.GetType().Name}.{field} not found");
                    return null;
                }

                string secondary = perk.SecondaryDescription?.Value ?? "";
                perk.Initialize(
                    perk.Name.Value,
                    perk.Skill,
                    (int)perk.RequiredSkillValue,
                    perk.AlternativePerk,
                    primaryDescription ?? perk.PrimaryDescription.Value,
                    primaryRole ?? perk.PrimaryRole,
                    primaryBonus ?? perk.PrimaryBonus,
                    perk.PrimaryIncrementType,
                    secondary.Length == 0 ? "" : secondaryDescription ?? secondary,
                    secondaryRole ?? perk.SecondaryRole,
                    secondaryBonus ?? perk.SecondaryBonus,
                    perk.SecondaryIncrementType,
                    primaryTroopUsageMask ?? perk.PrimaryTroopUsageMask,
                    secondaryTroopUsageMask ?? perk.SecondaryTroopUsageMask);
                return perk;
            }
            catch (Exception e)
            {
                Debug.Print($"[PerkFixesAndRebalance] Failed to edit {field}: {e}");
                return null;
            }
        }
    }

    [HarmonyPatch(typeof(DefaultPerks), "InitializeAll")]
    internal static class DefaultPerksInitializePatch
    {
        // DefaultPerks.Instance is not assigned yet during a new campaign's InitializeAll, so read fields off __instance.
        private static void Postfix(DefaultPerks __instance) => PerkChanges.ApplyVanilla(__instance);
    }

    [HarmonyPatch(typeof(DefaultSkillEffects), "InitializeAll")]
    internal static class DefaultSkillEffectsInitializePatch
    {
        private static void Postfix(DefaultSkillEffects __instance) => PerkChanges.ApplySkillEffects(__instance);
    }

    [HarmonyPatch]
    internal static class NavalPerksInitializePatch
    {
        private static MethodBase Target
        {
            get
            {
                Type type = AccessTools.TypeByName("NavalDLC.CharacterDevelopment.NavalPerks");
                return type == null ? null : AccessTools.Method(type, "InitializeAll");
            }
        }

        private static bool Prepare() => Target != null;

        private static MethodBase TargetMethod() => Target;

        private static void Postfix(object __instance) => PerkChanges.ApplyNaval(__instance);
    }
}
