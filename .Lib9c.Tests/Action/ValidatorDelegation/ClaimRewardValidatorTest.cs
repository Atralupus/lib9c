#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ClaimRewardValidatorTest : ValidatorDelegationTestBase
{
    private interface IClaimRewardFixture
    {
        FungibleAssetValue TotalReward { get; }

        PrivateKey ValidatorKey { get; }

        FungibleAssetValue ValidatorBalance { get; }

        FungibleAssetValue ValidatorCash { get; }

        DelegatorInfo[] DelegatorInfos { get; }

        GuildParticipantInfo[] GuildParticipantInfos { get; }

        PrivateKey[] DelegatorKeys => DelegatorInfos.Select(i => i.Key).ToArray();

        PrivateKey[] GuildParticipantKeys => GuildParticipantInfos.Select(i => i.Key).ToArray();

        FungibleAssetValue[] DelegatorBalances => DelegatorInfos.Select(i => i.Balance).ToArray();

        FungibleAssetValue[] GuildParticipantBalances => GuildParticipantInfos.Select(i => i.Balance).ToArray();
    }

    public static IEnumerable<object[]> RandomSeeds => new List<object[]>
    {
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
    };

    [Fact]
    public void Serialization()
    {
        var action = new ClaimRewardValidator();
        var plainValue = action.PlainValue;

        var deserialized = new ClaimRewardValidator();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = DelegationCurrency * 10;
        var allocatedReward = RewardCurrency * 100;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, allocatedReward, ref height);

        // When
        var expectedBalance = allocatedReward;
        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        var claimRewardValidator = new ClaimRewardValidatorSelf();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = claimRewardValidator.Execute(actionContext);

        // Then
        var actualBalance = world.GetBalance(validatorKey.Address, RewardCurrency);

        Assert.Equal(expectedBalance, actualBalance);
    }

    [Theory]
    [InlineData(33.33)]
    [InlineData(11.11)]
    [InlineData(10)]
    [InlineData(1)]
    public void Execute_Theory_OneDelegator(decimal totalReward)
    {
        var delegatorInfos = new[]
        {
            new DelegatorInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
            },
        };

        var guildParticipantInfos = new[]
        {
            new GuildParticipantInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
                GuildMasterAddress = delegatorInfos[0].Key.Address,
            },
        };

        var fixture = new StaticFixture
        {
            DelegatorLength = 1,
            TotalReward = FungibleAssetValue.Parse(RewardCurrency, $"{totalReward}"),
            ValidatorKey = new PrivateKey(),
            ValidatorBalance = DelegationCurrency * 100,
            ValidatorCash = DelegationCurrency * 10,
            DelegatorInfos = delegatorInfos,
            GuildParticipantInfos = guildParticipantInfos,
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(11.11)]
    [InlineData(11.12)]
    [InlineData(33.33)]
    [InlineData(33.34)]
    [InlineData(34.27)]
    [InlineData(34.28)]
    [InlineData(34.29)]
    public void Execute_Theory_TwoDelegators(decimal totalReward)
    {
        var delegatorInfos = new[]
        {
            new DelegatorInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
            },
            new DelegatorInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
            },
        };

        var guildParticipantInfos = new[]
        {
            new GuildParticipantInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
                GuildMasterAddress = delegatorInfos[0].Key.Address,
            },
            new GuildParticipantInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
                GuildMasterAddress = delegatorInfos[1].Key.Address,
            },
        };

        var fixture = new StaticFixture
        {
            DelegatorLength = 2,
            TotalReward = FungibleAssetValue.Parse(RewardCurrency, $"{totalReward}"),
            ValidatorKey = new PrivateKey(),
            ValidatorBalance = DelegationCurrency * 100,
            ValidatorCash = DelegationCurrency * 10,
            DelegatorInfos = delegatorInfos,
            GuildParticipantInfos = guildParticipantInfos,
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(123)]
    [InlineData(34352535)]
    public void Execute_Theory_WithStaticSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [MemberData(nameof(RandomSeeds))]
    public void Execute_Theory_WithRandomSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    private void ExecuteWithFixture(IClaimRewardFixture fixture)
    {
        // Given
        var length = fixture.DelegatorInfos.Length;
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var delegatorKeys = fixture.DelegatorKeys;
        var guildParticipantInfos = fixture.GuildParticipantInfos;
        var guildParticipantKeys = fixture.GuildParticipantKeys;
        var delegatorBalances = fixture.DelegatorBalances;
        var height = 1L;
        var actionContext = new ActionContext();
        var validatorBalance = fixture.ValidatorBalance;
        var validatorCash = fixture.ValidatorCash;
        var totalReward = fixture.TotalReward;
        int seed = 0;
        world = EnsureToMintAsset(world, validatorKey, validatorBalance, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorCash, height++);
        world = EnsureToMintAssets(world, delegatorKeys, delegatorBalances, height++);
        world = delegatorKeys.Aggregate(world, (w, d) => EnsureMakeGuild(
                w, d.Address, validatorKey.Address, height++, seed++));
        world = guildParticipantInfos.Aggregate(world, (w, i) => EnsureJoinGuild(
                w, i.Key.Address, i.GuildMasterAddress, validatorKey.Address, height++));

        world = EnsureRewardAllocatedValidator(world, validatorKey, totalReward, ref height);

        // Calculate expected values for comparison with actual values.
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedTotalShares = expectedDelegatee.TotalShares;
        var expectedValidatorShare
            = expectedRepository.GetBond(expectedDelegatee, validatorKey.Address).Share;
        var expectedDelegatorShares = delegatorKeys
            .Select(item => expectedRepository.GetBond(expectedDelegatee, item.Address).Share)
            .ToArray();
        var expectedProposerReward
            = CalculatePropserReward(totalReward) + CalculateBonusPropserReward(1, 1, totalReward);
        var expectedReward = totalReward - expectedProposerReward;
        var expectedCommission = CalculateCommission(
            expectedReward, expectedDelegatee.CommissionPercentage);
        var expectedClaim = expectedReward - expectedCommission;
        var expectedValidatorClaim = CalculateClaim(
            expectedValidatorShare, expectedTotalShares, expectedClaim);
        var expectedDelegatorClaims = CreateArray(
            length,
            i => CalculateClaim(expectedDelegatorShares[i], expectedTotalShares, expectedClaim));
        var expectedValidatorBalance = validatorBalance;
        expectedValidatorBalance -= validatorCash;
        var expectedValidatorReward = expectedProposerReward;
        expectedValidatorReward += expectedCommission;
        expectedValidatorReward += expectedValidatorClaim;
        var expectedDelegatorBalances = CreateArray(length, i => DelegationCurrency * 0);
        var expectedRemainReward = totalReward;
        expectedRemainReward -= expectedProposerReward;
        expectedRemainReward -= expectedCommission;
        expectedRemainReward -= expectedValidatorClaim;
        for (var i = 0; i < length; i++)
        {
            expectedRemainReward -= expectedDelegatorClaims[i];
        }

        // When
        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimRewardValidatorSelf().Execute(actionContext);
        for (var i = 0; i < length; i++)
        {
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = height++,
                Signer = delegatorKeys[i].Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator().Execute(actionContext);
        }

        // Then
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildRepository = new GuildRepository(world, actionContext);
        var delegatee = validatorRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualRemainReward = world.GetBalance(delegatee.RewardRemainderPoolAddress, RewardCurrency);
        var actualValidatorBalance = world.GetBalance(StakeState.DeriveAddress(validatorKey.Address), DelegationCurrency);
        var actualValidatorReward = world.GetBalance(validatorKey.Address, RewardCurrency);
        var actualDelegatorBalances = delegatorKeys
            .Select(item => world.GetBalance(item.Address, DelegationCurrency))
            .ToArray();
        var actualDelegatorRewards = delegatorKeys
            .Select(item => world.GetBalance(
                guildRepository.GetJoinedGuild(
                    new AgentAddress(item.Address))
                ?? throw new Exception($"Delegator {item.Address} does not joind to guild."),
                RewardCurrency))
            .ToArray();
        Assert.Equal(expectedRemainReward, actualRemainReward);
        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedDelegatorBalances, actualDelegatorBalances);
        Assert.Equal(expectedValidatorReward, actualValidatorReward);
        Assert.Equal(expectedDelegatorClaims, actualDelegatorRewards);

        foreach (var key in guildParticipantKeys)
        {
            Assert.Throws<InvalidOperationException>(
                () => new ClaimRewardValidator().Execute(new ActionContext
                {
                    PreviousState = world,
                    BlockIndex = height++,
                    Signer = key.Address,
                    LastCommit = lastCommit,
                }));
        }
    }

    private struct DelegatorInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Balance { get; set; }
    }

    private struct GuildParticipantInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Balance { get; set; }

        public Address GuildMasterAddress { get; set; }
    }

    private struct StaticFixture : IClaimRewardFixture
    {
        public int DelegatorLength { get; set; }

        public FungibleAssetValue TotalReward { get; set; }

        public PrivateKey ValidatorKey { get; set; }

        public FungibleAssetValue ValidatorBalance { get; set; }

        public FungibleAssetValue ValidatorCash { get; set; }

        public DelegatorInfo[] DelegatorInfos { get; set; }

        public GuildParticipantInfo[] GuildParticipantInfos { get; set; }
    }

    private class RandomFixture : IClaimRewardFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            DelegatorLength = _random.Next(3, 100);
            GuildParticipantLength = _random.Next(1, 50);
            ValidatorKey = new PrivateKey();
            TotalReward = GetRandomFAV(RewardCurrency, _random);
            ValidatorBalance = GetRandomFAV(DelegationCurrency, _random);
            ValidatorCash = GetRandomCash(_random, ValidatorBalance);
            DelegatorInfos = CreateArray(DelegatorLength, _ =>
            {
                var balance = GetRandomFAV(DelegationCurrency, _random);
                return new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                };
            });
            GuildParticipantInfos = CreateArray(GuildParticipantLength, _ =>
            {
                var balance = GetRandomFAV(DelegationCurrency, _random);
                return new GuildParticipantInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                    GuildMasterAddress = DelegatorInfos[_random.Next(DelegatorLength)].Key.Address,
                };
            });
        }

        public int DelegatorLength { get; }

        public int GuildParticipantLength { get; }

        public FungibleAssetValue TotalReward { get; }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorBalance { get; }

        public FungibleAssetValue ValidatorCash { get; }

        public DelegatorInfo[] DelegatorInfos { get; }

        public GuildParticipantInfo[] GuildParticipantInfos { get; }
    }
}
