﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reusable.Exceptionize;
using Reusable.SmartConfig.Data;
using Reusable.Tester;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;

namespace Reusable.SmartConfig.Tests
{
    [TestClass]
    public class ConfigurationTest
    {
        private ISettingConverter _relaySettingConverter;
        private ISettingDataStore _dictionaryDataStore;

        [TestInitialize]
        public void TestInitialize()
        {
            _relaySettingConverter = Mock.Create<ISettingConverter>();
            _relaySettingConverter
                .Arrange(x => x.Deserialize(Arg.IsAny<object>(), Arg.IsAny<Type>()))
                .Returns((Func<object, Type, object>)((value, type) => value));

            var testSettings = new Dictionary<SoftString, ISetting>
            {
                ["Setting1"] = new Setting { Name = "foo", Value = "foo-value" },
                ["Setting2"] = new Setting { Name = "bar", Value = "bar-value" },
            };

            _dictionaryDataStore = Mock.Create<ISettingDataStore>();
            _dictionaryDataStore
                .Arrange(x => x.Read(Arg.IsAny<SoftString>(), Arg.IsAny<Type>()))
                .Returns((Func<SoftString, Type, ISetting>)((name, type) => testSettings.TryGetValue(name, out var value) ? value : null));
        }

        [TestMethod]
        public void GetValue_NameExists_Value()
        {
            var setting = new Setting { Name = "foo", Value = "bar" };
            var dataStore = Mock.Create<ISettingDataStore>();
            var settingFinder = Mock.Create<ISettingFinder>();
            settingFinder
                .Arrange(x => x
                    .FindSetting(
                        Arg.IsAny<IEnumerable<ISettingDataStore>>(),
                        Arg.IsAny<SoftString>(),
                        Arg.IsAny<Type>(),
                        Arg.IsAny<SettingName>()
                    )
                )
                .Returns((dataStore, setting));

            var configuration = new Configuration(new [] { dataStore }, settingFinder);

            var actualValue = configuration.GetValue("foo", typeof(int), null);

            Assert.AreEqual("bar", actualValue);
        }

        [TestMethod]
        public void GetValue_NameDoesNotExist_Throws()
        {
            var configuration = new Configuration(new[]
            {
                _dictionaryDataStore
            });

            var ex = Assert.That.ThrowsExceptionFiltered<DynamicException>(() => configuration.GetValue("Setting3", typeof(int), null));
            Assert.AreEqual("Setting 'Setting3' not found.", ex.Message);
        }
    }
}