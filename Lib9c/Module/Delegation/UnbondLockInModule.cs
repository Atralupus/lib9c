#nullable enable
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Delegation;
using Nekoyume.Extensions;

namespace Nekoyume.Module.Delegation
{
    public static class UnbondLockInModule
    {
        public static UnbondLockIn GetUnbondLockIn(
            this IWorldState world, IDelegatee delegatee, Address delegatorAddress)
            => GetUnbondLockIn(
                world,
                delegatee.UnbondLockInAddress(delegatorAddress),
                delegatee.MaxUnbondLockInEntries);

        public static UnbondLockIn GetUnbondLockIn(
            this IWorldState world, Address address, int maxEntries)
            => TryGetUnbondLockIn(world, address, maxEntries, out var unbondLockIn)
                ? unbondLockIn!
                : new UnbondLockIn(address, maxEntries);

        public static bool TryGetUnbondLockIn(
            this IWorldState world,
            Address address,
            int maxEntries,
            out UnbondLockIn? unbondLockIn)
        {
            try
            {
                var value = world.GetAccountState(Addresses.UnbondLockIn).GetState(address);
                if (!(value is List list))
                {
                    unbondLockIn = null;
                    return false;
                }

                unbondLockIn = new UnbondLockIn(address, maxEntries, list);
                return true;
            }
            catch
            {
                unbondLockIn = null;
                return false;
            }
        }

        public static IWorld SetUnbondLockIn(
            this IWorld world, UnbondLockIn unbondLockIn)
            => world.MutateAccount(
                Addresses.UnbondLockIn,
                account => unbondLockIn.IsEmpty
                    ? account.RemoveState(unbondLockIn.Address)
                    : account.SetState(unbondLockIn.Address, unbondLockIn.Bencoded));
    }
}
