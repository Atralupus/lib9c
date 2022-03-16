// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class RankingSimulator : Simulator
    {
        private readonly EnemyPlayer _enemyPlayer;
        private readonly int _stageId;
        private List<ItemBase> _reward;

        public readonly WeeklyArenaRewardSheet WeeklyArenaRewardSheet;
        public override IEnumerable<ItemBase> Reward => _reward;


        public RankingSimulator(
            IRandom random,
            Player player,
            EnemyPlayerDigest enemyPlayerDigest,
            List<Guid> foods,
            RankingSimulatorSheets rankingSimulatorSheets,
            int stageId,
            CostumeStatSheet costumeStatSheet
        ) : base(
            random,
            player,
            foods,
            rankingSimulatorSheets
        )
        {
            _enemyPlayer = new EnemyPlayer(enemyPlayerDigest, CharacterSheet, CharacterLevelSheet, EquipmentItemSetEffectSheet)
            {
                Simulator = this
            };
            _enemyPlayer.Stats.EqualizeCurrentHPWithHP();
            _stageId = stageId;
            WeeklyArenaRewardSheet = rankingSimulatorSheets.WeeklyArenaRewardSheet;
            if (!(costumeStatSheet is null))
            {
                Player.SetCostumeStat(costumeStatSheet);
                _enemyPlayer.SetCostumeStat(costumeStatSheet);
            }
        }
        public RankingSimulator(
            IRandom random,
            AvatarState avatarState,
            AvatarState enemyAvatarState,
            List<Guid> foods,
            RankingSimulatorSheets rankingSimulatorSheets,
            int stageId
        ) : this(
            random,
            new Player(avatarState, rankingSimulatorSheets),
            new EnemyPlayerDigest(enemyAvatarState),
            foods,
            rankingSimulatorSheets,
            stageId,
            null
        )
        {
        }

        public RankingSimulator(
            IRandom random,
            AvatarState avatarState,
            AvatarState enemyAvatarState,
            List<Guid> foods,
            RankingSimulatorSheets rankingSimulatorSheets,
            int stageId,
            CostumeStatSheet costumeStatSheet
        ) : this(
            random,
            avatarState,
            enemyAvatarState,
            foods,
            rankingSimulatorSheets,
            stageId
        )
        {
            Player.SetCostumeStat(costumeStatSheet);
            _enemyPlayer.SetCostumeStat(costumeStatSheet);
        }

        public Player Simulate()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Log.stageId = _stageId;
            Spawn();
            Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            Characters.Enqueue(Player, TurnPriority / Player.SPD);
            Characters.Enqueue(_enemyPlayer, TurnPriority / _enemyPlayer.SPD);
            TurnNumber = 1;
            WaveNumber = 1;
            WaveTurn = 1;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(WaveNumber)} Start");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            while (true)
            {
                if (TurnNumber > MaxTurn)
                {
                    Result = BattleLog.Result.TimeOver;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(MaxTurn)}: {MaxTurn}");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                    break;
                }

                // 캐릭터 큐가 비어 있는 경우 break.
                if (!Characters.TryDequeue(out var character))
                    break;

                character.Tick();

                // 플레이어가 죽은 경우 break;
                if (Player.IsDead)
                {
                    Result = BattleLog.Result.Lose;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(Player)} Dead");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                    break;
                }

                // 플레이어의 타겟(적)이 없는 경우 break.
                if (!Player.Targets.Any())
                {
                    Result = BattleLog.Result.Win;
                    Log.clearedWaveNumber = WaveNumber;

                    break;
                }

                foreach (var other in Characters)
                {
                    var current = Characters.GetPriority(other);
                    var speed = current * 0.6m;
                    Characters.UpdatePriority(other, speed);
                }

                Characters.Enqueue(character, TurnPriority / character.SPD);
            }
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(Simulate)} End");
            sb.Append($" / {nameof(Result)}: {Result.ToString()}");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV1()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Log.stageId = _stageId;
            SpawnV1();
            Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            Characters.Enqueue(Player, TurnPriority / Player.SPD);
            Characters.Enqueue(_enemyPlayer, TurnPriority / _enemyPlayer.SPD);
            TurnNumber = 1;
            WaveNumber = 1;
            WaveTurn = 1;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(WaveNumber)} Start");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            while (true)
            {
                if (TurnNumber > MaxTurn)
                {
                    Result = BattleLog.Result.TimeOver;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(MaxTurn)}: {MaxTurn}");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                    break;
                }

                // 캐릭터 큐가 비어 있는 경우 break.
                if (!Characters.TryDequeue(out var character))
                    break;

                character.Tick();

                // 플레이어가 죽은 경우 break;
                if (Player.IsDead)
                {
                    Result = BattleLog.Result.Lose;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(Player)} Dead");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                    break;
                }

                // 플레이어의 타겟(적)이 없는 경우 break.
                if (!Player.Targets.Any())
                {
                    Result = BattleLog.Result.Win;
                    Log.clearedWaveNumber = WaveNumber;

                    break;
                }

                foreach (var other in Characters)
                {
                    var current = Characters.GetPriority(other);
                    var speed = current * 0.6m;
                    Characters.UpdatePriority(other, speed);
                }

                Characters.Enqueue(character, TurnPriority / character.SPD);
            }
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(Simulate)} End");
            sb.Append($" / {nameof(Result)}: {Result.ToString()}");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            return Player;
        }

        [Obsolete("Use Simulate")]
        public Player SimulateV2()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Log.stageId = _stageId;
            SpawnV2();
            Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            Characters.Enqueue(Player, TurnPriority / Player.SPD);
            Characters.Enqueue(_enemyPlayer, TurnPriority / _enemyPlayer.SPD);
            TurnNumber = 1;
            WaveNumber = 1;
            WaveTurn = 1;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(WaveNumber)} Start");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            while (true)
            {
                if (TurnNumber > MaxTurn)
                {
                    Result = BattleLog.Result.TimeOver;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(MaxTurn)}: {MaxTurn}");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                    break;
                }

                // 캐릭터 큐가 비어 있는 경우 break.
                if (!Characters.TryDequeue(out var character))
                    break;

                character.Tick();

                // 플레이어가 죽은 경우 break;
                if (Player.IsDead)
                {
                    Result = BattleLog.Result.Lose;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(Player)} Dead");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                    break;
                }

                // 플레이어의 타겟(적)이 없는 경우 break.
                if (!Player.Targets.Any())
                {
                    Result = BattleLog.Result.Win;
                    Log.clearedWaveNumber = WaveNumber;

                    break;
                }

                foreach (var other in Characters)
                {
                    var current = Characters.GetPriority(other);
                    var speed = current * 0.6m;
                    Characters.UpdatePriority(other, speed);
                }

                Characters.Enqueue(character, TurnPriority / character.SPD);
            }
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(Simulate)} End");
            sb.Append($" / {nameof(Result)}: {Result.ToString()}");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            return Player;
        }

        private void Spawn()
        {
            Player.Spawn();
            _enemyPlayer.Spawn();
            Player.Targets.Add(_enemyPlayer);
            _enemyPlayer.Targets.Add(Player);
        }

        [Obsolete("Use Spawn")]
        private void SpawnV1()
        {
            Player.SpawnV1();
            _enemyPlayer.SpawnV1();
            Player.Targets.Add(_enemyPlayer);
            _enemyPlayer.Targets.Add(Player);
        }

        [Obsolete("Use Spawn")]
        private void SpawnV2()
        {
            Player.SpawnV2();
            _enemyPlayer.SpawnV2();
            Player.Targets.Add(_enemyPlayer);
            _enemyPlayer.Targets.Add(Player);
        }

        public void SetReward(List<ItemBase> reward)
        {
            _reward = reward;
        }
    }
}
