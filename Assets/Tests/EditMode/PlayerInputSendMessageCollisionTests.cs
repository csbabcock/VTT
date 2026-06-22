using System;
using System.Linq;
using System.Reflection;
using GameCore;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Guards against PlayerInput SendMessages double-invoking handlers.
    ///
    /// The player prefab's <c>PlayerInput</c> uses Send Messages notification. For each action
    /// named e.g. <c>TogglePerspective</c>, Unity also calls <c>OnTogglePerspective()</c> on
    /// the GameObject. <see cref="PlayerInputs"/> separately raises C# events for the same
    /// actions, so handler methods on <see cref="PlayerController"/> must NOT use the
    /// <c>On{ActionName}</c> pattern or each key press toggles twice (the V-key bug).
    /// </summary>
    public class PlayerInputSendMessageCollisionTests
    {
        private static readonly (string ActionName, string ForbiddenHandlerName)[] GuardedActions =
        {
            ("TogglePerspective", "OnTogglePerspective"),
            ("ToggleEncounterMode", "OnToggleEncounterMode"),
        };

        [Test]
        public void PlayerController_DoesNotDefineSendMessageCollidingHandlerNames()
        {
            var methods = typeof(PlayerController).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var (actionName, forbiddenName) in GuardedActions)
            {
                bool hasCollision = methods.Any(m =>
                    m.DeclaringType == typeof(PlayerController) && m.Name == forbiddenName);

                Assert.IsFalse(
                    hasCollision,
                    $"PlayerController must not define {forbiddenName}(). " +
                    $"PlayerInput SendMessages invokes that name for action '{actionName}', " +
                    "which double-fires alongside PlayerInputs C# events.");
            }
        }

        [Test]
        public void PlayerController_DefinesNonCollidingPerspectiveHandler()
        {
            var method = typeof(PlayerController).GetMethod(
                "HandlePerspectiveToggle",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(method, "Expected HandlePerspectiveToggle to handle perspective toggles.");
        }

        [Test]
        public void PlayerController_DefinesNonCollidingEncounterToggleHandler()
        {
            var method = typeof(PlayerController).GetMethod(
                "HandleToggleEncounterMode",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(method, "Expected HandleToggleEncounterMode to handle encounter toggles.");
        }
    }
}
