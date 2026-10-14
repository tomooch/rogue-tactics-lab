using NUnit.Framework;
using RogueTactics.Core.G1;
using RogueTactics.Presentation;

namespace RogueTactics.Tests
{
    public class MobileG1SessionTests
    {
        [TestCase(Preference.ProtectAnchor,"e6f35225a2e812bc9e1e12521abb2ae94546650fceb8b8e64e2bf3950718b15d")]
        [TestCase(Preference.DirectAttack,"85770bb4c9e2c4cd5247a515e15bbdb3995dd6d20d4f44917fae832c109c9c2e")]
        public void MobileAdapterPreservesFrozenG1Trace(Preference preference,string expected)
        {
            var session=new MobileG1Session();session.Select(preference);session.Start("自分の予想");
            while(session.Replay.State.Outcome==Outcome.Active)session.Advance();
            Assert.That(session.Replay.Hash(),Is.EqualTo(expected));Assert.That(session.Trials[preference].Prediction,Is.EqualTo("自分の予想"));
            session.Reset();Assert.That(session.Replay.State.Serialize(),Is.EqualTo(Fixture.Baseline().Serialize()));Assert.That(session.Trials.Count,Is.EqualTo(1));
        }
    }
}
