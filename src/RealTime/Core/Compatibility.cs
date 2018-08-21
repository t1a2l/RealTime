﻿// <copyright file="Compatibility.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework.Plugins;
    using ICities;
    using RealTime.Localization;
    using SkyTools.Localization;
    using SkyTools.Tools;

    /// <summary>
    /// An utility class for checking the compatibility with other installed mods.
    /// </summary>
    internal sealed class Compatibility
    {
        /// <summary>The Workshop ID of the 'CitizenLifecycleRebalance' mod.</summary>
        public const ulong CitizenLifecycleRebalanceId = 654707599;
        private static readonly ulong[] IncompatibleModIds =
        {
            605590542,  // Rush Hour II
            629713122,  // Climate Control
            702070768,  // Export Electricity
            649522495,  // District Service Limit
            1181352643, // District Service Limit 3.0
        };

        private readonly string modName;
        private readonly ILocalizationProvider localizationProvider;
        private readonly Dictionary<ulong, PluginManager.PluginInfo> activeMods;

        private Compatibility(string modName, ILocalizationProvider localizationProvider)
        {
            this.modName = modName;
            this.localizationProvider = localizationProvider;
            activeMods = new Dictionary<ulong, PluginManager.PluginInfo>();
        }

        /// <summary>Initializes a new instance of the <see cref="Compatibility"/> class.</summary>
        /// <param name="modName">The name of the current mod.</param>
        /// <param name="localizationProvider">The localization provider to use for translation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="modName"/> is null or an empty string.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="localizationProvider"/> is null.</exception>
        /// <returns>A new and initialized instance of the <see cref="Compatibility"/> class.</returns>
        public static Compatibility Create(string modName, ILocalizationProvider localizationProvider)
        {
            if (string.IsNullOrEmpty(modName))
            {
                throw new ArgumentException("The mod name cannot be null or an empty string", nameof(modName));
            }

            var result = new Compatibility(modName, localizationProvider ?? throw new ArgumentNullException(nameof(localizationProvider)));
            result.Initialize();
            return result;
        }

        /// <summary>Checks for enabled incompatible mods and prepares a notification message text if any found.</summary>
        /// <param name="message">The translated message text about incompatible mods. If none found, <c>null</c>.</param>
        /// <returns><c>true</c> if there are any active incompatible mod detected; otherwise, <c>false</c>.</returns>
        public bool AreAnyIncompatibleModsActive(out string message)
        {
            List<string> incompatibleMods = GetIncompatibleModNames();
            if (incompatibleMods.Count == 0)
            {
                message = null;
                return false;
            }

            string separator = Environment.NewLine + " - ";
            message = localizationProvider.Translate(TranslationKeys.IncompatibleModsFoundMessage)
                + Environment.NewLine + separator
                + string.Join(separator, incompatibleMods.ToArray())
                + Environment.NewLine + Environment.NewLine;

            return true;
        }

        /// <summary>
        /// Determines whether a mod with any of the specified Workshop IDs is currently installed and enabled.
        /// </summary>
        /// <param name="modId">The mod ID to check.</param>
        /// <param name="furtherModIds">Further mod IDs to check.</param>
        /// <returns><c>true</c> if a mod with any of the specified Workshop ID is currently installed and enabled;
        /// otherwise, <c>false</c>.</returns>
        public bool IsAnyModActive(ulong modId, params ulong[] furtherModIds)
        {
            return activeMods.ContainsKey(modId) || furtherModIds?.Any(activeMods.ContainsKey) == true;
        }

        private List<string> GetIncompatibleModNames()
        {
            var result = new List<string>();
            foreach (ulong modId in IncompatibleModIds)
            {
                try
                {
                    if (activeMods.TryGetValue(modId, out PluginManager.PluginInfo mod))
                    {
                        result.Add((mod.userModInstance as IUserMod)?.Name ?? mod.publishedFileID.AsUInt64.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"The 'Real Time' mod wanted to check compatibility but failed, error message: {ex}");
                }
            }

            return result;
        }

        private void Initialize()
        {
            activeMods.Clear();
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo().Where(m => m.isEnabled))
            {
                activeMods[plugin.publishedFileID.AsUInt64] = plugin;
            }
        }
    }
}
