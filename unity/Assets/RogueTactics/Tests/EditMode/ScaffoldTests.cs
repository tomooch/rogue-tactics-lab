using System;
using System.Linq;
using NUnit.Framework;
using RogueTactics.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RogueTactics.Tests
{
    public class ScaffoldTests
    {
        [Test]
        public void SameCoordinateOffsetsProduceSameSequence()
        {
            var offsets = new[] { new GridPosition(1, 2), new GridPosition(-3, 0), new GridPosition(0, -4) };
            GridPosition[] Replay()
            {
                var state = new GridPosition(0, 0);
                return offsets.Select(input => state = state.Offset(input.X, input.Y)).ToArray();
            }
            var first = Replay();
            CollectionAssert.AreEqual(new[] { new GridPosition(1, 2), new GridPosition(-2, 2), new GridPosition(-2, -2) }, first);
            CollectionAssert.AreEqual(first, Replay());
        }
        [Test] public void CoreReferencesNeitherUnityNorPresentation()
        {
            var names = typeof(GridPosition).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.That(names.Any(n => n.StartsWith("Unity") || n.StartsWith("RogueTactics.Presentation")), Is.False);
        }
        [Test] public void CoordinateOverflowFailsExplicitly()
            => Assert.Throws<OverflowException>(() => new GridPosition(int.MaxValue, 0).Offset(1, 0));
        [Test] public void PortraitConfigurationIsPinned()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(390));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(844));
            Assert.That(EditorBuildSettings.scenes.Single().path, Is.EqualTo("Assets/RogueTactics/Scenes/Stage0.unity"));
        }
        [Test] public void UrpIsConfigured() => Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
    }
}
