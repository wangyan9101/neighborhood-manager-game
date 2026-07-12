using System.Linq;
using NeighborhoodManager.Core;
using NeighborhoodManager.Models;
using NeighborhoodManager.Scene;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeighborhoodManager.Tests
{
    public sealed class Task4CameraAndSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game_MVP.unity";

        [Test]
        public void PositionInsideLimitsRemainsUnchanged()
        {
            Vector3 position = new Vector3(2f, 24f, -10f);

            Vector3 result = CameraController.ClampPosition(position,
                new Vector2(-18f, 18f), new Vector2(-28f, 12f));

            Assert.That(result, Is.EqualTo(position));
        }

        [Test]
        public void PositionClampsXAndZWithoutChangingHeight()
        {
            Vector3 result = CameraController.ClampPosition(new Vector3(-30f, 24f, 30f),
                new Vector2(-18f, 18f), new Vector2(-28f, 12f));

            Assert.That(result.x, Is.EqualTo(-18f));
            Assert.That(result.y, Is.EqualTo(24f));
            Assert.That(result.z, Is.EqualTo(12f));
        }

        [Test]
        public void ReversedPositionLimitsAreHandledSafely()
        {
            Vector3 result = CameraController.ClampPosition(new Vector3(50f, 7f, -50f),
                new Vector2(18f, -18f), new Vector2(12f, -28f));

            Assert.That(result, Is.EqualTo(new Vector3(18f, 7f, -28f)));
        }

        [TestCase(50f, 35f, 70f, 50f)]
        [TestCase(20f, 35f, 70f, 35f)]
        [TestCase(80f, 35f, 70f, 70f)]
        [TestCase(60f, 45f, 45f, 45f)]
        [TestCase(20f, 70f, 35f, 35f)]
        [TestCase(80f, 70f, 35f, 70f)]
        public void ZoomClampHandlesNormalFixedAndReversedLimits(
            float value, float minimum, float maximum, float expected)
        {
            Assert.That(CameraController.ClampZoom(value, minimum, maximum), Is.EqualTo(expected));
        }

        [TestCase(true, GamePhase.Playing, false, true)]
        [TestCase(false, GamePhase.DaySettlement, false, true)]
        [TestCase(false, GamePhase.Playing, true, true)]
        [TestCase(false, GamePhase.Playing, false, false)]
        public void InputBlockingUsesUiPhaseAndOverlayState(
            bool pointerOverUi, GamePhase phase, bool overlayVisible, bool expected)
        {
            Assert.That(CameraController.ShouldBlockInput(pointerOverUi, phase, overlayVisible), Is.EqualTo(expected));
        }

        [Test]
        public void MainSceneHasCompleteDemo02Wiring()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameRoot gameRoot = roots.SelectMany(root => root.GetComponentsInChildren<GameRoot>(true)).Single();
            CameraController controller = roots.SelectMany(root =>
                root.GetComponentsInChildren<CameraController>(true)).Single();
            Camera mainCamera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Single(camera => camera.CompareTag("MainCamera"));
            int uiEventSystems = roots.SelectMany(root =>
                root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count();
            int missingScripts = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));

            Assert.That(gameRoot.ValidateReferences(), Is.True);
            Assert.That(gameRoot.EventConfigs, Has.Count.EqualTo(9));
            Assert.That(gameRoot.EventConfigs.All(item => item != null), Is.True);
            Assert.That(gameRoot.EventConfigs.Select(item => item.EventId).Distinct().Count(), Is.EqualTo(9));
            Assert.That(controller.HasRequiredReferences, Is.True);
            Assert.That(uiEventSystems, Is.EqualTo(1));
            Assert.That(missingScripts, Is.EqualTo(0));
            Assert.That(mainCamera.orthographic, Is.False);
            Assert.That(mainCamera.fieldOfView, Is.EqualTo(60f));
            Assert.That(mainCamera.transform.position, Is.EqualTo(new Vector3(0f, 24f, -24f)));
            Assert.That(Quaternion.Angle(mainCamera.transform.rotation, Quaternion.Euler(42f, 0f, 0f)),
                Is.LessThan(0.01f));
        }
    }
}
