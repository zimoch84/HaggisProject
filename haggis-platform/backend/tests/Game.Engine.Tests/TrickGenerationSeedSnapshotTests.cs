using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace HaggisTests
{
    [TestFixture]
    internal class TrickGenerationSeedSnapshotTests
    {
        private sealed class TestableTrickGenerationService : TrickGenerationServiceBase
        {
            public List<Trick> Opening(IHaggisPlayer player) => BuildPossibleOpeningTricks(player);
        }

        private static readonly Dictionary<int, string[]> HandsBySeed = new()
        {
            [1] = new[] { "9B", "5G", "8O", "2Y", "9Y", "8R", "6B", "10B", "9R", "9O", "8B", "2R", "2G", "7B", "J", "Q", "K" },
            [2] = new[] { "3G", "8O", "2G", "3Y", "6O", "8G", "6B", "7R", "2Y", "7O", "5R", "7Y", "9B", "7G", "J", "Q", "K" },
            [3] = new[] { "7Y", "5O", "9O", "9B", "9Y", "3G", "8O", "8B", "4Y", "7B", "8G", "6Y", "9G", "3Y", "J", "Q", "K" }
        };

        private static readonly Dictionary<int, string[]> ExpectedBySeed = new()
        {
            [1] = DecodeExpected("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"),
            [2] = DecodeExpected("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"),
            [3] = DecodeExpected("H4sIAAAAAAAEAI1ayZIbNwy952vs6UXS0SpPUCMmoelxUkGp9Cf98VFzw8pWbs2HB3ARARCY+fz4C/54v0/w+O2zfmL/nOlzif1zJfR0pU9Cz4Seye6ZLFyIcCHChRHI2K1/pf4VHr/9+Pbx87nqbV9t+77dpweNghglNkLBRMFEzpx35sxGQYwSjZb4ZC5sFMQo0Wjdba5sFMQo0eh03U5I37f7iUmCGCU2QsFEwUTOPF+3/efp35G+b/czYwUxSmwETAuEFggtEFpRMKNgRs68XLcLsO/IvpG+b/cL0whilNgImAVgFkBYAGEBhIUomFEwo2AimwuFFgotrFq/fn78yH649espoGChpKBdy/AyqJnJMtFTR08dHfXqKNU/FJgUmAyzek91GgUmBSbDrC5VPUmBSYHJMLOfVacRULBQUtCuZXgZ1Mxkmeipo6eOjnp23+x/AiiuJaBgoaShaBWjVYxGcdcyvAxqZrJMsPOCnRfsvODNC9684M0bPfXoqUdHPUel7N4SQAXcuGMXKFgoaShaxWgVo1VEvSa0ltBaQmNp1zK8DGpmskywOwC7A7A7AL0DsDsAuwOwOwBvB+DtALwdRE89eurRU0d7AGgPAO0BoDcvevMin/fzPU332/3tseXIPksIt/yI24GcDeYWUAu0i+vLpSpNJWBv+cG3Q5Wwrdg5WVwja4ZyrF5b1KqsrJOfMATk6JtfjTuUxTUAVE4Wb+crB3Lsyy9JBbUlZo3t0pSqzeyRVSUbyI8JBsR83TKQ1Z9aX7+0pWS6RGL5ZQmpszxXQ1gzs4NfOwgeGD0QJZj+/vadvQpq2tVo0miy3E4UOPo4z901V2ecJ+WahDPOs23Nrhmn3FqTl0aTRpPldqLA0cdbWqzXSmLBwZLGWG7QqOEmhxtdC9G1ED0LnShwcC2DaxlcyzCwHH285bkazCQWHCwZrAR0iVhr6FhDa41FR40abnK40bUQXQvRtYDOaaBzGuicBrqzozs7erN3osDB3Re4+wJ3X+DsC5x9gbMvcPcF7r7A3RcM9hUHOLr7RXe/6O4XjeVn4J1FBq2RjeE5TyaF1qBJyXIWObbGwYL3RNvy6CxyLecSsSXPWeTcGu0I75O1zDqLhNzy5Mxyco0WhCaZeTkaWPqdWc5u+XVmabsl4Vlk7pZ4Bdqz79zzN0uuM8viLL/OLJc7cCy/OMHMLEuzM8/uEqccL3CxkueJkESa30VvXQRjURyL0BPt3YKn+K3cvNCv3wQHMtSyVC/5WEK9MC1DLWvvj+cNnFHieXrWEWD2Ju5ljqz2B4TF1jTId3KJQidz+XtEykJ3EKWXkU103ISEvWSkLHTHWOUprsW3qGsn8HzprSR0h9h9V8qaT1IHL8vKO0l06RpevID6egbnZ9AeW/lun+Xp5PWUF8H5aiS4iY6ekLBXhJSF8lo7P+TqtMyso8UQISmvL9Yf7GjcWKewocV9qWtY8PxKYl3BhhY2SHY5pVryXq5GUt8cFzCS2N5BBi8aRhLKa+zykCto750aG748lCweyHAkgwM9ONDDsd6tnBDFSkcKh9J4KEUpLUXx568n6Y1XPy008E5jZbUoNLFmZI44itX8s7tv8W3LSuT8ofm5YLHapl1H6twRp7uXadIpTuqObu3c+lKbA4v+lWIx1ytx43+w/BmxvRTIkwWrV1y6gSbk3VV5R40Y3aVUw6sxgBjgMm59q82Vs8/6nEickR3mrFe74hYbuiVn343DLDl7p8jQbTm7YzVbd03XUVg59pqond0nwpi4v7ztLQ90ifuNYUx2H+jSUEVpf0/+c8EBT/5k5awdpjzs63BuUMy+yt8//nn/LgKT7rVwhmqK1KzPGaK1UX16LE9jeTrS170AzbDdAsGAVwz9Q6hfzR6d6EPUkm0sT2N5OtRnRfBIeqw7nBlfrBxfrByPV67LWc2wpbRm6KJ6LPfXqAttKz9cAbxcI7xYI7xYI7xYI7xcI3prfD5KFt0gaPX9Imv+VsovsrynOnyxNX4r3Bdb5rc6fdGVfiuqF13st2p90fV+K9m5oCXyWrcvsupnFfYia38roQ5AywVaFotnkUx2A1gZvpieAKvEF9MZcIRRvC270EzHCvLF9gqkVHcMuvTj3/fvTkyuIbGIbexqV4uL9d0fCNNAODY7ntW/70VsJiV3WB13aFd/NR5Bt3913KLd2NX1jHbRV9c52qVeHf9gl211vMSKha8IseMW7GKtnnMIuXfVVcdnYl0dVrKw/sgkOzFOL2Tq/7VR6yHTSZl0j2UT/5n02f+ARz0T+g8JyeD9EdUNme5K1/QeJtlH2cT/Gwl5ok5EcBi8d6I6IiMGX4XosajOwSQ7LaoLIaW6GzHprovqDUy695Jdi/cbGqP3YFSnYtJdE9XhmHjvxK3gJ95HGTNSP5nKCZpDs4+s8B0MrIhzGpYfk19h+2Xi5JfaftU5+TW3X8TSj6NKYb8anWxNfFg9TcfVk3xCP0/v5EfjloF2ghuSW+Q9+VFZ/UbzyKvV3Z5931beNY88XN3zeeTnyh9HPO2Z891ZlXsn5/7vXNaTjpjSoyrzGv+87vH7GVdP5Rw4gtnzC4KNAxW5bal/hfqV+tdT+vz+DxkqWjrfKwAA")
        };

        private TestableTrickGenerationService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new TestableTrickGenerationService();
        }

        [Test]
        public void OpeningTricks_FirstHand_Seed1_ShouldMatchSnapshot()
        {
            AssertOpeningSnapshot(1);
        }

        [Test]
        public void OpeningTricks_FirstHand_Seed2_ShouldMatchSnapshot()
        {
            AssertOpeningSnapshot(2);
        }

        [Test]
        public void OpeningTricks_FirstHand_Seed3_ShouldMatchSnapshot()
        {
            AssertOpeningSnapshot(3);
        }

        private void AssertOpeningSnapshot(int seed)
        {
            var player = new HaggisPlayer($"seed-{seed}") { Hand = HandsBySeed[seed].ToCards() };
            var tricks = _service.Opening(player);
            tricks.Sort((left, right) =>
            {
                var comparison = left.CompareTo(right);
                return comparison != 0 ? comparison : string.CompareOrdinal(left.ToString(), right.ToString());
            });

            var actual = tricks.Select(trick => trick.ToString()).ToArray();
            var expected = ExpectedBySeed[seed];

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static string[] DecodeExpected(string base64Gzip)
        {
            var compressedBytes = Convert.FromBase64String(base64Gzip);
            using var input = new MemoryStream(compressedBytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            var content = Encoding.UTF8.GetString(output.ToArray());
            return content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
