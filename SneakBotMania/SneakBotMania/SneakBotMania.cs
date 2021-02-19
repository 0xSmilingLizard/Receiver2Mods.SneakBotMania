using System;

using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using Receiver2;

using UnityEngine;

namespace SneakBotMania
{
    [BepInProcess("Receiver2.exe")]
    [BepInPlugin("SmilingLizard.plugins.sneakbotmania", "SneakBot Mania", "2.0")]
    public class SneakBotMania : BaseUnityPlugin
    {
        private ConfigEntry<KeyboardShortcut> _manualSpawnCfg;
        private ConfigEntry<int> _minimumSpawnCfg;
        private ConfigEntry<int> _maximumSpawnCfg;
        private ConfigEntry<float> _initialTimerCfg;
        private ConfigEntry<float> _minTimerCfg;
        private ConfigEntry<float> _maxTimerCfg;
        private ConfigEntry<bool> _enableTimeCfg;
        private System.Random _rng;

        private static SneakBotMania _Instance { get; set; }

        private float _timer = -1f;

        int _MinAmount => this._minimumSpawnCfg.Value;
        private int _MaxAmount => this._maximumSpawnCfg.Value;
        bool _RandomAmount => this._maximumSpawnCfg.Value > this._MinAmount;

        private int _SpawnAmount => this._RandomAmount ? _GetRandomizedAmount() : this._MinAmount;

        float _MinTimer => this._minTimerCfg.Value;
        private float _Maxtimer => this._maxTimerCfg.Value;
        private bool _TimerEnabled => this._enableTimeCfg.Value && this._maxTimerCfg.Value > 0;
        private bool _TimerRandom => 0f < this._MinTimer && this._MinTimer < this._maxTimerCfg.Value;

        public void Awake()
        {
            _Instance = this;
            this._manualSpawnCfg = this.Config.Bind(section: "Keybind",
                                                    key: "Manual Spawn",
                                                    defaultValue: new KeyboardShortcut(KeyCode.None),
                                                    description: "Spawns SneakBots; amount determined same as normal; circumvents any spawn limits.");
            this._minimumSpawnCfg = this.Config.Bind(section: "Spawn Amount",
                                                     key: "Minimum",
                                                     defaultValue: 1,
                                                     configDescription: new ConfigDescription("The minimum amount of SneakBots to spawn whenever any are spawned.",
                                                                                              new AcceptableValueRange<int>(1, 50)));
            this._maximumSpawnCfg = this.Config.Bind(section: "Spawn Amount",
                                                     key: "Maximum",
                                                     defaultValue: 1,
                                                     configDescription: new ConfigDescription("The maximum amount of SneakBots to spawn whenever any are spawned. If this is equal or smaller than the minimum amount, then the amount spawned isn't random but equal the minimum value instead.",
                                                                                              new AcceptableValueRange<int>(1, 50)));
            this._initialTimerCfg = this.Config.Bind(section: "Timer",
                                                          key: "Timer Initial Duration",
                                                          defaultValue: 0f,
                                                          configDescription: new ConfigDescription("The length of the timer is randomly set whenever it elapses. This is the duration it starts with. Set to zero to determine inital duration by normal rules.",
                                                                                          new AcceptableValueRange<float>(0f, 600f)));
            this._minTimerCfg = this.Config.Bind(section: "Timer",
                                                 key: "Timer Minimum Duration",
                                                 defaultValue: 0f,
                                                 configDescription: new ConfigDescription("This is the minimum duration it can be set to. Set to zero or a value grater than the maximum to make the timer not random.",
                                                                                          new AcceptableValueRange<float>(0f, 600f)));
            this._maxTimerCfg = this.Config.Bind(section: "Timer",
                                                 key: "Timer Maximum Duration",
                                                 defaultValue: 0f,
                                                 configDescription: new ConfigDescription("This is the maximum duration it can be set to. Set to zero to disable the timer.",
                                                                                          new AcceptableValueRange<float>(0f, 600f)));
            this._enableTimeCfg = this.Config.Bind("Timer",
                                           "Enable Timer",
                                           false,
                                           "Enables the timer. Use this to disable it so that you can change its durations without immediately spawning a SneakBot.");

            this._timer = this._initialTimerCfg.Value > 0 ? this._initialTimerCfg.Value : _GetNextTimerDuration();

            this._rng = new System.Random();
            _ = Harmony.CreateAndPatchAll(typeof(SneakBotMania));
        }

        public void Update()
        {
            if (this._TimerEnabled)
            {
                if (RuntimeTileLevelGenerator.instance is null)
                {
                    this._timer = this._initialTimerCfg.Value;
                }
                else
                {
                    this._timer -= Time.deltaTime;
                    if (this._timer <= 0f)
                    {
                        this._timer = _GetNextTimerDuration();
                        _ = RuntimeTileLevelGenerator.instance.CreateSneakBot();
                    }
                }
            }
            if (this._manualSpawnCfg.Value.IsDown())
            {
                if (RuntimeTileLevelGenerator.instance is null)
                {
                    this.Logger.LogWarning("Failed to spawn SneakBots because no RuntimeTileLevelGenerator instance was in the scene.");
                    return;
                }

                SneakBotLoop();
            }
        }

        private float _GetNextTimerDuration()
        {
            return this._TimerRandom ? (float)this._rng.NextDouble() * this._Maxtimer + this._MinTimer : this._MinTimer;
        }

        private int _GetRandomizedAmount()
        {
            return this._RandomAmount ? this._rng.Next(this._MinAmount, this._MaxAmount + 1) : this._MinAmount;
        }

        public void SneakBotLoop()
        {
            if (RuntimeTileLevelGenerator.instance is null)
            {
                this.Logger.LogWarning("Failed to spawn SneakBots because no RuntimeTileLevelGenerator instance was in the scene.");
                return;
            }

            int toBeSpawned = this._SpawnAmount;
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
            _Instance.SneakBotLoop();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(RuntimeTileLevelGenerator), nameof(RuntimeTileLevelGenerator.CreateSneakBot))]
        public static ActiveEnemy OriginalSneakBot(RuntimeTileLevelGenerator instance)
        {
            throw new NotImplementedException("This method failed to be replaced with CreateSneakBot().");
        }
    }
}