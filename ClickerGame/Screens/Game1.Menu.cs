// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace ClickerGame;
public partial class Game1
{
        void ExecuteMenuAction(string key)
        {
                if (key == "menu_start") StartPlaying(false);
                else if (key == "menu_editor")
                {
                    state = GameState.BeatmapEditor;
                    menuMusicInstance?.Stop();
                    InitEditor();
                    discordRpc?.SetEditor("");
                }
                else if (key == "menu_stats")
                {
                    state = GameState.Stats;
                    string? user = accountsManager?.LoggedInUser;
                    cachedStats = statsDb?.GetSummary(user);
                    cachedRecent = statsDb?.GetRecentPlays(user, 8);
                    discordRpc?.SetStats();
                }
                else if (key == "menu_profile")
                {
                    state = GameState.Profile;
                    profileScrollIndex = 0;
                    var user = accountsManager?.LoggedInUser;
                    if (user != null)
                    {
                        // Build local fallback profile immediately
                        viewingProfile = new PlayerProfileDto { User = user };
                        // Apply local saved profile data
                        var lp = LocalProfileData.Load();
                        viewingProfile.AvatarId = lp.AvatarId;
                        viewingProfile.BannerId = lp.BannerId;
                        viewingProfile.Bio = lp.Bio;
                        viewingProfile.Region = lp.Region;
                        if (statsDb != null)
                        {
                            var summary = statsDb.GetSummary(user);
                            if (summary != null)
                            {
                                viewingProfile.TotalPlays = summary.TotalPlays;
                                viewingProfile.BestCombo = summary.BestCombo;
                                viewingProfile.AvgAccuracy = summary.AvgAccuracy;
                            }
                        }
                        // Try cloud fetch to enrich with badges/bio/region
                        if (cloudSync != null)
                        {
                            profileLoading = true;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var cloud = await cloudSync.GetProfileAsync(user);
                                    if (cloud != null) viewingProfile = cloud;
                                }
                                catch { }
                                finally { profileLoading = false; }
                            });
                        }
                    }
                    else
                    {
                        viewingProfile = null;
                    }
                }
                else if (key == "menu_search")
                {
                    state = GameState.SearchPlayer;
                    searchQuery = ""; searchResults.Clear(); searchSelectedIndex = 0; searchLoading = false;
                }
                else if (key == "menu_settings")
                {
                    state = GameState.Settings;
                    settingsMenuIndex = 0;
                    settingsBindingMode = false;
                }
                else if (key == "menu_achievements")
                {
                    state = GameState.Achievements;
                    achievementsScrollIndex = 0;
                }
                else if (key == "menu_account")
                {
                    state = GameState.Account;
                    accountUsername = ""; accountPassword = "";
                    accountShowMessage = false; accountFieldIndex = 0;
                    accountIsLoginMode = true;
                }
                else if (key == "menu_language")
                {
                    state = GameState.Language;
                    languageMenuIndex = Array.IndexOf(Localization.All, Localization.Current);
                    if (languageMenuIndex < 0) languageMenuIndex = 0;
                }
                else if (key == "menu_exit") Exit();
        }

}
