﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Users;
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens
{
    internal class LeaderboardScreen : PerformanceCalculatorScreen
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Green);

        private LimitedLabelledNumberBox playerAmountTextBox;
        private LimitedLabelledNumberBox pageTextBox;
        private StatefulButton calculationButton;
        private VerboseLoadingLayer loadingLayer;

        private FillFlowContainer leaderboardContainer;

        private CancellationTokenSource calculationCancellatonToken;

        public override bool ShouldShowConfirmationDialogOnSwitch => leaderboardContainer.Count > 0;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; }

        [Resolved]
        private APIManager apiManager { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private SettingsManager configManager { get; set; }

        private const int settings_height = 40;

        public LeaderboardScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 40), new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                Name = "Settings",
                                Height = settings_height,
                                RelativeSizeAxes = Axes.X,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(),
                                    new Dimension(),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        playerAmountTextBox = new LimitedLabelledNumberBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.TopLeft,
                                            Label = "Amount",
                                            PlaceholderText = "10",
                                            MinValue = 1,
                                            MaxValue = 50,
                                            Value = { Value = 10 },
                                            CommitOnFocusLoss = false
                                        },
                                        pageTextBox = new LimitedLabelledNumberBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.TopLeft,
                                            Label = "Page",
                                            PlaceholderText = "1",
                                            MinValue = 1,
                                            Value = { Value = 1 },
                                            CommitOnFocusLoss = false
                                        },
                                        calculationButton = new StatefulButton("Start calculation")
                                        {
                                            Width = 150,
                                            Height = settings_height,
                                            Action = calculate
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = leaderboardContainer = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(5),
                                    Padding = new MarginPadding(15)
                                }
                            }
                        },
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();
            calculationCancellatonToken = null;
        }

        private void calculate()
        {
            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();

            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            leaderboardContainer.Clear();

            calculationCancellatonToken = new CancellationTokenSource();
            var token = calculationCancellatonToken.Token;

            Task.Run(async () =>
            {
                Schedule(() => loadingLayer.Text.Value = "Getting leaderboard...");

                var leaderboard = await apiManager.GetJsonFromApi<GetTopUsersResponse>($"rankings/{ruleset.Value.ShortName}/performance?cursor[page]={pageTextBox.Value.Value - 1}");

                var calculatedPlayers = new List<(string, decimal, decimal)>();

                for (int i = 0; i < playerAmountTextBox.Value.Value; i++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var player = leaderboard.Users[i];

                    Schedule(() => loadingLayer.Text.Value = $"Calculating {player.User.Username} top scores...");

                    var playerData = await calculatePlayer(player, token);

                    calculatedPlayers.Add((player.User.Username, playerData.LocalPP, playerData.LivePP));

                    Schedule(() =>
                    {
                        var playerPanel = new UserPPListPanel(player.User);
                        leaderboardContainer.Add(playerPanel);

                        playerPanel.Data.Value = playerData;
                    });
                }

                var localOrderedPlayers = calculatedPlayers.OrderByDescending(x => x.Item2).ToList();
                var liveOrderedPlayers = calculatedPlayers.OrderByDescending(x => x.Item3).ToList();

                Schedule(() =>
                {
                    foreach (var calculatedPlayer in calculatedPlayers)
                    {
                        leaderboardContainer.SetLayoutPosition(leaderboardContainer[liveOrderedPlayers.IndexOf(calculatedPlayer)], localOrderedPlayers.IndexOf(calculatedPlayer));
                    }
                });
            }, token).ContinueWith(t =>
            {
                Logger.Log(t.Exception?.ToString(), level: LogLevel.Error);
                notificationDisplay.Display(new Notification(t.Exception?.Flatten().Message));
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    loadingLayer.Hide();
                    calculationButton.State.Value = ButtonState.Done;
                });
            }, token);
        }

        private async Task<UserPPListPanelData> calculatePlayer(UserStatistics player, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return new UserPPListPanelData();

            var plays = new List<ExtendedScore>();

            var apiScores = await apiManager.GetJsonFromApi<List<APIScore>>($"users/{player.User.OnlineID}/scores/best?mode={ruleset.Value.ShortName}&limit=100");

            var rulesetInstance = ruleset.Value.CreateInstance();

            try
            {
                Parallel.ForEach(apiScores, new ParallelOptions { CancellationToken = token }, score =>
                {
                    try
                    {
                        var working = ProcessorWorkingBeatmap.FromFileOrId(score.Beatmap?.OnlineID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                        var modsAcronyms = score.Mods.Select(x => x.ToString()).ToArray();
                        Mod[] mods = rulesetInstance.CreateAllMods().Where(m => modsAcronyms.Contains(m.Acronym)).ToArray();

                        var scoreInfo = new ScoreInfo(working.BeatmapInfo, ruleset.Value)
                        {
                            TotalScore = score.TotalScore,
                            MaxCombo = score.MaxCombo,
                            Mods = mods,
                            Statistics = new Dictionary<HitResult, int>()
                        };

                        scoreInfo.SetCount300(score.Statistics["count_300"]);
                        scoreInfo.SetCountGeki(score.Statistics["count_geki"]);
                        scoreInfo.SetCount100(score.Statistics["count_100"]);
                        scoreInfo.SetCountKatu(score.Statistics["count_katu"]);
                        scoreInfo.SetCount50(score.Statistics["count_50"]);
                        scoreInfo.SetCountMiss(score.Statistics["count_miss"]);

                        var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                        var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                        var difficultyAttributes = difficultyCalculator.Calculate(RulesetHelper.ConvertToLegacyDifficultyAdjustmentMods(rulesetInstance, mods));
                        var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();

                        var livePp = score.PP ?? 0.0;
                        var perfAttributes = performanceCalculator?.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                        score.PP = perfAttributes?.Total ?? 0.0;

                        var extendedScore = new ExtendedScore(score, livePp, perfAttributes);
                        plays.Add(extendedScore);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), level: LogLevel.Error);
                        notificationDisplay.Display(new Notification(e.Message));

                        // should probably stop calculating??
                    }
                });
            }
            catch (OperationCanceledException) { }

            var localOrdered = plays.OrderByDescending(x => x.PP).ToList();
            var liveOrdered = plays.OrderByDescending(x => x.LivePP).ToList();

            int index = 0;
            decimal totalLocalPP = (decimal)localOrdered.Select(x => x.PP).Sum(play => Math.Pow(0.95, index++) * play);
            decimal totalLivePP = player.PP ?? (decimal)0.0;

            index = 0;
            decimal nonBonusLivePP = (decimal)liveOrdered.Select(x => x.LivePP).Sum(play => Math.Pow(0.95, index++) * play);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;

            return new UserPPListPanelData
            {
                LivePP = totalLivePP,
                LocalPP = totalLocalPP,
                PlaycountPP = playcountBonusPP
            };
        }
    }
}
