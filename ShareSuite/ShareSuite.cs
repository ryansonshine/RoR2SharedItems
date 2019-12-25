﻿using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable UnusedMember.Local

namespace ShareSuite
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.funkfrog_sipondo.sharesuite", "ShareSuite", "1.13.4")]
    [R2APISubmoduleDependency("CommandHelper","ItemDropAPI")]
    public class ShareSuite : BaseUnityPlugin
    {
        #region ConfigWrapper init

        public static ConfigEntry<bool> ModIsEnabled,
            MoneyIsShared,
            WhiteItemsShared,
            GreenItemsShared,
            RedItemsShared,
            EquipmentShared,
            LunarItemsShared,
            BossItemsShared,
            PrinterCauldronFixEnabled,
            DeadPlayersGetItems,
            OverridePlayerScalingEnabled,
            OverrideBossLootScalingEnabled,
            MoneyScalarEnabled;

        public static ConfigEntry<int> BossLootCredit;
        public static ConfigEntry<double> InteractablesCredit, MoneyScalar;
        public static ConfigEntry<string> ItemBlacklist, EquipmentBlacklist;


        private bool previouslyEnabled = false;
        #endregion

        public static HashSet<int> GetItemBlackList()
        {
            var blacklist = new HashSet<int>();
            var rawPieces = ItemBlacklist.Value.Split(',');
            foreach (var piece in rawPieces)
            {
                if (int.TryParse(piece, out var itemNum))
                {
                    blacklist.Add(itemNum);
                }
            }

            return blacklist;
        }

        public static HashSet<int> GetEquipmentBlackList()
        {
            var blacklist = new HashSet<int>();
            var rawPieces = EquipmentBlacklist.Value.Split(',');
            foreach (var index in rawPieces.Select(x => int.TryParse(x, out var i) ? i : -1)) blacklist.Add(index);
            return blacklist;
        }

        public void Update()
        {
            if (!NetworkServer.active
                || !MoneyIsShared.Value
                || MoneySharingHooks.MapTransitionActive) return;

            foreach (var playerCharacterMasterController in PlayerCharacterMasterController.instances)
            {
                if (!playerCharacterMasterController.master.alive) continue;
                if (playerCharacterMasterController.master.money != MoneySharingHooks.SharedMoneyValue)
                {
                    playerCharacterMasterController.master.money = (uint) MoneySharingHooks.SharedMoneyValue;
                }
            }
        }

        public ShareSuite()
        {
            InitConfig();
            CommandHelper.AddToConsoleWhenReady();

            #region Hook registration

            // Register all the hooks
            ReloadHooks();
            MoneySharingHooks.SharedMoneyValue = 0;

            #endregion
        }

        private void ReloadHooks(object _ = null, System.EventArgs __= null)
        {
            if(previouslyEnabled && !ModIsEnabled.Value)
            {
                GeneralHooks.UnHook();
                MoneySharingHooks.UnHook();
                ItemSharingHooks.UnHook();
                EquipmentSharingHooks.UnHook();
                previouslyEnabled = false;
            }
            if (!previouslyEnabled && ModIsEnabled.Value)
            {
                previouslyEnabled = true;
                GeneralHooks.Hook();
                MoneySharingHooks.Hook();
                ItemSharingHooks.Hook();
                EquipmentSharingHooks.Hook();
            }
        }

        private void InitConfig()
        {
            ModIsEnabled = Config.Bind(
                "Settings",
                "ModEnabled",
                true,
                "Toggles mod."
                );
            ModIsEnabled.SettingChanged += ReloadHooks;

            MoneyIsShared = Config.Bind(
                "Settings",
                "MoneyShared",
                true,
                "Toggles money sharing."
                );

            WhiteItemsShared = Config.Bind(
                "Settings",
                "WhiteItemsShared",
                true,
                "Toggles item sharing for common items."
                );

            GreenItemsShared = Config.Bind(
                "Settings",
                "GreenItemsShared",
                true,
                "Toggles item sharing for rare items."
                );

            RedItemsShared = Config.Bind(
                "Settings",
                "RedItemsShared",
                true,
                "Toggles item sharing for legendary items."
                );

            EquipmentShared = Config.Bind(
                "Settings",
                "EquipmentShared",
                false,
                "Toggles item sharing for equipment."
                );

            LunarItemsShared = Config.Bind(
                "Settings",
                "LunarItemsShared",
                false,
                "Toggles item sharing for Lunar items."
                );

            BossItemsShared = Config.Bind(
                "Settings",
                "BossItemsShared",
                true,
                "Toggles item sharing for boss items."
                );

            PrinterCauldronFixEnabled = Config.Bind(
                "Balance",
                "PrinterCauldronFix",
                true,
                "Toggles 3D printer and Cauldron item dupe fix by giving the item directly instead of" +
                " dropping it on the ground."
                );

            DeadPlayersGetItems = Config.Bind(
                "Balance",
                "DeadPlayersGetItems",
                false,
                "Toggles item sharing for dead players."
                );

            OverridePlayerScalingEnabled = Config.Bind(
                "Balance",
                "OverridePlayerScaling",
                true,
                "Toggles override of the scalar of interactables (chests, shrines, etc) that spawn in the world to your configured credit."
                );

            InteractablesCredit = Config.Bind(
                "Balance",
                "InteractablesCredit",
                1d,
                "If player scaling via this mod is enabled, the amount of players the game should think are playing in terms of chest spawns."
                );

            OverrideBossLootScalingEnabled = Config.Bind(
                "Balance",
                "OverrideBossLootScaling",
                true,
                "Toggles override of the scalar of boss loot drops to your configured balance."
                );

            BossLootCredit = Config.Bind(
                "Balance",
                "BossLootCredit",
                1,
                "Specifies the amount of boss items dropped when boss drop override is true."
                );

            MoneyScalarEnabled = Config.Bind(
                "Settings",
                "MoneyScalarEnabled",
                false,
                "Toggles money scalar."
                );

            MoneyScalar = Config.Bind(
                "Settings",
                "MoneyScalar",
                1D,
                "Modifies player count used in calculations of gold earned when money sharing is on."
                );

            ItemBlacklist = Config.Bind(
                "Settings",
                "ItemBlacklist",
                "53,60,82,86",
                "Items (by index) that you do not want to share, comma separated. Please find the item indices at: https://github.com/risk-of-thunder/R2Wiki/wiki/Item-&-Equipment-IDs-and-Names"
                );

            EquipmentBlacklist = Config.Bind(
                "Settings",
                "EquipmentBlacklist",
                "",
                "Equipment (by index) that you do not want to share, comma separated. Please find the indices at: https://github.com/risk-of-thunder/R2Wiki/wiki/Item-&-Equipment-IDs-and-Names"
                );
        }

        private static bool TryParseIntoConfig<T>(string rawValue, ConfigEntry<T> wrapper)
        {
            switch (wrapper)
            {
                case ConfigEntry<bool> boolWrapper when bool.TryParse(rawValue, out bool result):
                    boolWrapper.Value = result;
                    return true;
                case ConfigEntry<int> intWrapper when int.TryParse(rawValue, out int result):
                    intWrapper.Value = result;
                    return true;
                default:
                    return false;
            }
        }

        #region CommandParser
#pragma warning disable IDE0051 //Commands usually aren't called from code.

        // ModIsEnabled
        [ConCommand(commandName = "ss_Enabled", flags = ConVarFlags.None, helpText = "Toggles mod.")]
        private static void CcModIsEnabled(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], ModIsEnabled))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Mod status set to {ModIsEnabled.Value}.");
        }

        // MoneyIsShared
        [ConCommand(commandName = "ss_MoneyIsShared", flags = ConVarFlags.None,
            helpText = "Modifies whether money is shared or not.")]
        private static void CcMoneyIsShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], MoneyIsShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Money sharing set to {MoneyIsShared.Value}.");
        }

        // MoneyScalarEnabled
        [ConCommand(commandName = "ss_MoneyScalarEnabled", flags = ConVarFlags.None,
            helpText = "Modifies whether the money scalar is enabled.")]
        private static void CcMoneyScalarEnabled(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], MoneyScalarEnabled))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Money scaling toggle set to {MoneyScalarEnabled.Value}.");
        }

        // MoneyScalar
        [ConCommand(commandName = "ss_MoneyScalar", flags = ConVarFlags.None,
            helpText = "Modifies percent of gold earned when money sharing is on.")]
        private static void CcMoneyScalar(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], MoneyScalar))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Money multiplier set to {MoneyScalar.Value}.");
        }

        // WhiteItemsShared
        [ConCommand(commandName = "ss_WhiteItemsShared", flags = ConVarFlags.None,
            helpText = "Modifies whether white items are shared or not.")]
        private static void CcWhiteShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], WhiteItemsShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"White item sharing set to {WhiteItemsShared.Value}.");
        }

        // GreenItemsShared
        [ConCommand(commandName = "ss_GreenItemsShared", flags = ConVarFlags.None,
            helpText = "Modifies whether green items are shared or not.")]
        private static void CcGreenShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], GreenItemsShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Green item sharing set to {GreenItemsShared.Value}.");
        }

        // RedItemsShared
        [ConCommand(commandName = "ss_RedItemsShared", flags = ConVarFlags.None,
            helpText = "Modifies whether red items are shared or not.")]
        private static void CcRedShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], RedItemsShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Red item sharing set to {RedItemsShared.Value}.");
        }

        // EquipmentShared
        [ConCommand(commandName = "ss_EquipmentShared", flags = ConVarFlags.None,
            helpText = "Modifies whether equipment is shared or not.")]
        private static void CcEquipmentShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], EquipmentShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Equipment sharing set to {EquipmentShared.Value}.");
        }

        // LunarItemsShared
        [ConCommand(commandName = "ss_LunarItemsShared", flags = ConVarFlags.None,
            helpText = "Modifies whether lunar items are shared or not.")]
        private static void CcLunarShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], LunarItemsShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Lunar item sharing set to {LunarItemsShared.Value}.");
        }

        // BossItemsShared
        [ConCommand(commandName = "ss_BossItemsShared", flags = ConVarFlags.None,
            helpText = "Modifies whether boss items are shared or not.")]
        private static void CcBossShared(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], BossItemsShared))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Boss item sharing set to {BossItemsShared.Value}.");
        }

        // PrinterCauldronFix
        [ConCommand(commandName = "ss_PrinterCauldronFix", flags = ConVarFlags.None,
            helpText = "Modifies whether printers and cauldrons should not duplicate items.")]
        private static void CcPrinterCauldronFix(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], PrinterCauldronFixEnabled))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Printer and cauldron fix set to {PrinterCauldronFixEnabled.Value}.");
        }

        // DisablePlayerScaling
        [ConCommand(commandName = "ss_OverridePlayerScaling", flags = ConVarFlags.None,
            helpText = "Modifies whether interactable count should scale based on player count.")]
        private static void CcDisablePlayerScaling(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], OverridePlayerScalingEnabled))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Player scaling disable set to {OverridePlayerScalingEnabled.Value}.");
        }

        // InteractablesCredit
        [ConCommand(commandName = "ss_InteractablesCredit", flags = ConVarFlags.None,
            helpText = "Modifies amount of interactables when player scaling is overridden.")]
        private static void CcInteractablesCredit(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], InteractablesCredit))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Interactables credit set to {InteractablesCredit.Value}.");
        }

        // DisableBossLootScaling
        [ConCommand(commandName = "ss_OverrideBossLootScaling", flags = ConVarFlags.None,
            helpText = "Modifies whether boss loot should scale based on player count.")]
        private static void CcBossLoot(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], OverrideBossLootScalingEnabled))
                Debug.Log("Invalid arguments.");
            else
            {
                Debug.Log($"Boss loot scaling disable set to {OverrideBossLootScalingEnabled.Value}.");
            }
        }

        // BossLootCredit
        [ConCommand(commandName = "ss_BossLootCredit", flags = ConVarFlags.None,
            helpText = "Modifies amount of boss item drops.")]
        private static void CcBossLootCredit(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], BossLootCredit))
                Debug.Log("Invalid arguments.");
            else
            {
                Debug.Log($"Boss loot credit set to {BossLootCredit.Value}.");
            }
        }

        // DeadPlayersGetItems
        [ConCommand(commandName = "ss_DeadPlayersGetItems", flags = ConVarFlags.None,
            helpText = "Modifies whether boss loot should scale based on player count.")]
        private static void CcDeadPlayersGetItems(ConCommandArgs args)
        {
            if (args.Count != 1 || !TryParseIntoConfig(args[0], DeadPlayersGetItems))
                Debug.Log("Invalid arguments.");
            else
                Debug.Log($"Boss loot scaling disable set to {DeadPlayersGetItems.Value}.");
        }
#pragma warning restore IDE0051
        #endregion CommandParser
    }
}