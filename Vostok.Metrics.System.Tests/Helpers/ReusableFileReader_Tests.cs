using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Tests.Helpers
{
    [TestFixture]
    internal class ReusableFileReader_Tests
    {
        [Test]
        public void Should_be_reusable()
        {
            var path = Path.GetTempFileName();
            using var reader = new ReusableFileReader(path);

            for (var i = 1; i < 10; i++)
            {
                var data = Enumerable.Range(0, i).Select(i => $"{i} = {Guid.NewGuid()}").ToArray();
                File.WriteAllText(path, string.Join(Environment.NewLine, data));

                reader.ReadFirstLine().Should().Be(data[0]);
                reader.ReadLines().Should().BeEquivalentTo(data);
            }
        }
    }
}