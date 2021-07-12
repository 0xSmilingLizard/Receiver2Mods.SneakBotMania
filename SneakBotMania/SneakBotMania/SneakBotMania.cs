using System;

using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using Receiver2;
using Receiver2.SneakBot;

using UnityEngine;

namespace SneakBotMania
{
    [BepInProcess("Receiver2.exe")]
    [BepInPlugin("SmilingLizard.plugins.sneakbotmania", "SneakBot Mania", "2.1")]
    public class SneakBotMania : BaseUnityPlugin
    {
        private ConfigEntry<KeyboardShortcut> manualSpawnCfg;
        private ConfigEntry<int> minimumSpawnCfg;
        private ConfigEntry<int> maximumSpawnCfg;
        private ConfigEntry<float> initialTimerCfg;
        private ConfigEntry<float> minTimerCfg;
        private ConfigEntry<float> maxTimerCfg;
        private ConfigEntry<bool> enableTimeCfg;
        private ConfigEntry<bool> unlimitVanilla;
        private ConfigEntry<bool> alternateGong;

        private System.Random rng;

        private static SneakBotMania Instance { get; set; }

        private float timer = -1f;

        private int MinAmount => this.minimumSpawnCfg.Value;
        private int MaxAmount => this.maximumSpawnCfg.Value;
        private bool RandomAmount => this.maximumSpawnCfg.Value > this.MinAmount;

        private int SpawnAmount => this.RandomAmount ? GetRandomizedAmount() : this.MinAmount;

        private float MinTimer => this.minTimerCfg.Value;
        private float Maxtimer => this.maxTimerCfg.Value;
        private bool TimerEnabled => this.enableTimeCfg.Value && this.maxTimerCfg.Value > 0;
        private bool TimerRandom => 0f < this.MinTimer && this.MinTimer < this.maxTimerCfg.Value;

        public void Awake()
        {
            Instance = this;
            this.manualSpawnCfg = this.Config.Bind(
                section: "Keybind",
                key: "Manual Spawn",
                defaultValue: new KeyboardShortcut(KeyCode.None),
                description: "Spawns SneakBots; amount determined same as normal; circumvents any spawn limits.");
            this.minimumSpawnCfg = this.Config.Bind(
                section: "Spawn Amount",
                key: "Minimum",
                defaultValue: 1,
                configDescription: new ConfigDescription(
                    "The minimum amount of SneakBots to spawn whenever any are spawned.",
                    new AcceptableValueRange<int>(1, 50)));
            this.maximumSpawnCfg = this.Config.Bind(
                section: "Spawn Amount",
                key: "Maximum",
                defaultValue: 1,
                configDescription: new ConfigDescription(
                    "The maximum amount of SneakBots to spawn whenever any are spawned. If this is equal or smaller than the minimum amount, then the amount spawned isn't random but equal the minimum value instead.",
                    new AcceptableValueRange<int>(1, 50)));
            this.initialTimerCfg = this.Config.Bind(
                section: "Timer",
                key: "Timer Initial Duration",
                defaultValue: 0f,
                configDescription: new ConfigDescription(
                    "The length of the timer is randomly set whenever it elapses. This is the duration it starts with. Set to zero to determine inital duration by normal rules.",
                    new AcceptableValueRange<float>(0f, 600f)));
            this.minTimerCfg = this.Config.Bind(
                section: "Timer",
                key: "Timer Minimum Duration",
                defaultValue: 0f,
                configDescription: new ConfigDescription(
                    "This is the minimum duration it can be set to. Set to zero or a value grater than the maximum to make the timer not random.",
                    new AcceptableValueRange<float>(0f, 600f)));
            this.maxTimerCfg = this.Config.Bind(
                section: "Timer",
                key: "Timer Maximum Duration",
                defaultValue: 0f,
                configDescription: new ConfigDescription(
                    "This is the maximum duration it can be set to. Set to zero to disable the timer.",
                    new AcceptableValueRange<float>(0f, 600f)));
            this.enableTimeCfg = this.Config.Bind(
                "Timer",
                "Enable Timer",
                false,
                "Enables the timer. Use this to disable it so that you can change its durations without immediately spawning a SneakBot.");
            this.unlimitVanilla = this.Config.Bind(
                section: "Overwrite",
                key: "Natural Limit",
                defaultValue: false,
                description: "Removes the limit of 1 per level from normal means of spawning sneakbots. (\"Alternate Gong Mechanic\" recommended)");
            this.alternateGong = this.Config.Bind(
                section: "Overwrite",
                key: "Alternate Gong Mechanic",
                defaultValue: false,
                description: "Changes the way the gong decides to spawn sneakbots to be more compatible with \"Natural Limit\".");

            this.timer = this.initialTimerCfg.Value > 0 ? this.initialTimerCfg.Value : GetNextTimerDuration();

            this.rng = new System.Random();
            _ = Harmony.CreateAndPatchAll(typeof(SneakBotMania));
        }

        public void Update()
        {
            if (this.TimerEnabled)
            {
                if (RuntimeTileLevelGenerator.instance is null)
                {
                    this.timer = this.initialTimerCfg.Value;
                }
                else
                {
                    this.timer -= Time.deltaTime;
                    if (this.timer <= 0f)
                    {
                        this.timer = GetNextTimerDuration();
                        _ = RuntimeTileLevelGenerator.instance.CreateSneakBot();
                    }
                }
            }
            if (this.manualSpawnCfg.Value.IsDown())
            {
                if (RuntimeTileLevelGenerator.instance is null)
                {
                    this.Logger.LogWarning("Failed to spawn SneakBots because no RuntimeTileLevelGenerator instance was in the scene.");
                    return;
                }

                SneakBotLoop();
            }
        }

        private float GetNextTimerDuration()
        {
            return this.TimerRandom ? (float)this.rng.NextDouble() * this.Maxtimer + this.MinTimer : this.MinTimer;
        }

        private int GetRandomizedAmount()
        {
            return this.RandomAmount ? this.rng.Next(this.MinAmount, this.MaxAmount + 1) : this.MinAmount;
        }

        public void SneakBotLoop()
        {
            if (RuntimeTileLevelGenerator.instance is null)
            {
                this.Logger.LogWarning("Failed to spawn SneakBots because no RuntimeTileLevelGenerator instance was in the scene.");
                return;
            }

            int toBeSpawned = this.SpawnAmount;
            this.Logger.LogInfo($"Spawning {toBeSpawned} SneakBots. Enjoy!");

            for (int i = 0; i < toBeSpawned - 1; i++)
            {
                _ = OriginalSneakBot(RuntimeTileLevelGenerator.instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RuntimeTileLevelGenerator), nameof(RuntimeTileLevelGenerator.CreateSneakBot))]
        [HarmonyAfter(nameof(OriginalSneakBot))]
        public static void SneakBotPatch()
        {
            Instance.SneakBotLoop();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(RuntimeTileLevelGenerator), nameof(RuntimeTileLevelGenerator.CreateSneakBot))]
        public static ActiveEnemy OriginalSneakBot(RuntimeTileLevelGenerator instance)
        {
            throw new NotImplementedException("This method failed to be replaced with CreateSneakBot().");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SneakBotSpawner), nameof(SneakBotSpawner.TrySpawnSneakBot))]
        public static bool UnlimitSpawner()
        {
            if (Instance.unlimitVanilla.Value)
            {
                RuntimeTileLevelGenerator rt = RuntimeTileLevelGenerator.instance;

                if (rt != null)
                {
                    RankingProgressionGameMode mode = ReceiverCoreScript.Instance() is null
                        ? null
                        : ReceiverCoreScript.Instance().game_mode as RankingProgressionGameMode;

                    if (mode != null
                        && !mode.progression_data.has_picked_up_sneakbot_tape
                        && !mode.progression_data.has_picked_up_mindcontrol_tape)
                    {
                        RankingProgressionGameMode.can_unlock_sneak_bot_note = true;
                    }

                    ReceiverOnScreenMessage.QueueMessage(Locale.GetUIString(LocaleUIString.SB_SPAWN_MESSAGE));
                    _ = rt.CreateSneakBot();

                    return false;
                }
            }
            return true;
        }

        static int count = 10;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CountTrigger), nameof(CountTrigger.Increment))]
        public static bool StaticCount(CountTrigger __instance)
        {
            if (Instance.alternateGong.Value)
            {
                if (__instance.gameObject.GetComponent<SneakBotSpawner>() is SneakBotSpawner sbs)
                {
                    if (--count <= 0)
                    {
                        count = Instance.rng.Next(1, 12);

                        if (Instance.rng.Next(100) is 0)
                        {
                            sbs.TrySpawnSneakBotInstanced();
                            sbs.TrySpawnSneakBotInstanced();
                            sbs.TrySpawnSneakBotInstanced();
                        }
                        else
                        {
                            sbs.TrySpawnSneakBotInstanced();
                        }
                    }

                    return false;
                }
            }
            return true;
        }
    }
}