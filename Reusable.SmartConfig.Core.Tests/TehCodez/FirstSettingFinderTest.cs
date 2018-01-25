﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reusable.Exceptionize;
using Reusable.SmartConfig.Data;
using Reusable.Tester;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;

namespace Reusable.SmartConfig.Tests
{
    [TestClass]
    public class FirstSettingFinderTest
    {
        [TestMethod]
        public void FindSetting_AnyDataSource_Finds()
        {
            var dataStore1 = Mock.Create<ISettingDataStore>();
            var dataStore2 = Mock.Create<ISettingDataStore>();
            var dataStore3 = Mock.Create<ISettingDataStore>();

            dataStore1
                .Arrange(x => x.Read(Arg.IsAny<SoftString>(), Arg.IsAny<Type>()))
                .Returns(default(ISetting));

            dataStore2
                .Arrange(x => x.Read(Arg.Matches<SoftString>(arg => arg == SoftString.Create("setting2")), Arg.IsAny<Type>()))
                .Returns(new Setting { Name = "setting2", Value = "bar" });

            dataStore3
                .Arrange(x => x.Read(Arg.IsAny<SoftString>(), Arg.IsAny<Type>()))
                .Returns(default(ISetting));

            var settingFinder = new FirstSettingFinder();
            var result = settingFinder.FindSetting(new[] { dataStore1, dataStore2, dataStore3 }, "setting2", typeof(string), null);

            Assert.AreSame(dataStore2, result.DataStore);
            Assert.AreEqual("bar", result.Setting.Value);
        }

        [TestMethod]
        public void FindSetting_NoDataSource_Throws()
        {
            var dataStore1 = Mock.Create<ISettingDataStore>();
           
            var settingFinder = new FirstSettingFinder();

            Assert.That.ThrowsExceptionFiltered<DynamicException>(() => settingFinder.FindSetting(new[] {dataStore1}, "setting2", typeof(string), null));
        }
    }
}