using NUnit.Framework;
using GameCore.Actors;
using GameCore.PlayerData;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="PlayerActor"/> data binding and display-name behavior used by
    /// the networked player spawner and DM tools.
    /// </summary>
    public class PlayerActorTests
    {
        private GameObject _gameObject;
        private PlayerActor _actor;

        [SetUp]
        public void SetUp()
        {
            ActorRegistry.Clear();
            PlayerDataServiceLocator.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);

            ActorRegistry.Clear();
            PlayerDataServiceLocator.Reset();
        }

        [Test]
        public void RemoteActor_DataServiceIsNull_WithoutInjection()
        {
            _gameObject = new GameObject("RemotePlayer");
            _actor = _gameObject.AddComponent<PlayerActor>();
            _actor.SetOwner(ownerId: 2, isLocalPlayer: false);

            Assert.IsNull(_actor.DataService);
            Assert.IsNull(_actor.Sheet);
        }

        [Test]
        public void LocalActor_FallsBackToPlayerDataServiceLocator()
        {
            var locatorService = new InMemoryPlayerDataService(
                new DnD5eCharacterData { characterName = "LocatorHero" });
            PlayerDataServiceLocator.Service = locatorService;

            _gameObject = new GameObject("LocalPlayer");
            _actor = _gameObject.AddComponent<PlayerActor>();

            Assert.AreSame(locatorService, _actor.DataService);
            Assert.AreEqual("LocatorHero", _actor.Sheet.CharacterName);
        }

        [Test]
        public void InjectedDataService_WinsOverLocator_ForRemoteActor()
        {
            PlayerDataServiceLocator.Service = new InMemoryPlayerDataService(
                new DnD5eCharacterData { characterName = "WrongSheet" });

            var injected = new InMemoryPlayerDataService(
                new DnD5eCharacterData { characterName = "RemoteHero" });

            _gameObject = new GameObject("RemotePlayer");
            _actor = _gameObject.AddComponent<PlayerActor>();
            _actor.SetDataService(injected);
            _actor.SetOwner(ownerId: 3, isLocalPlayer: false);

            Assert.AreSame(injected, _actor.DataService);
            Assert.AreEqual("RemoteHero", _actor.DisplayName);
        }

        [Test]
        public void SetDisplayName_OverridesSheetName()
        {
            var service = new InMemoryPlayerDataService(
                new DnD5eCharacterData { characterName = "SheetName" });

            _gameObject = new GameObject("Player");
            _actor = _gameObject.AddComponent<PlayerActor>();
            _actor.SetDataService(service);

            Assert.AreEqual("SheetName", _actor.DisplayName);

            _actor.SetDisplayName("ReplicatedName");
            Assert.AreEqual("ReplicatedName", _actor.DisplayName);
        }
    }
}
