﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reusable.Data.Repositories;
using Reusable.SmartConfig.Data;
using Reusable.SmartConfig.Internal;
using Reusable.Utilities.SqlClient;

namespace Reusable.SmartConfig
{
    public class SqlServer : SettingProvider
    {
        public const string DefaultSchema = "dbo";

        public const string DefaultTable = "Setting";

        private IReadOnlyDictionary<string, object> _where = new Dictionary<string, object>();

        private SqlFourPartName _settingTableName;

        private SqlServerColumnMapping _columnMapping;

        public SqlServer(string nameOrConnectionString, ISettingConverter converter) : base(converter)
        {
            ConnectionString = ConnectionStringRepository.Default.GetConnectionString(nameOrConnectionString);

            SettingTableName = (DefaultSchema, DefaultTable);
            ColumnMapping = new SqlServerColumnMapping();
        }

        [NotNull]
        public string ConnectionString { get; }

        [NotNull]
        public SqlFourPartName SettingTableName
        {
            get => _settingTableName;
            set => _settingTableName = value ?? throw new ArgumentNullException(nameof(SettingTableName));
        }

        [NotNull]
        public SqlServerColumnMapping ColumnMapping
        {
            get => _columnMapping;
            set => _columnMapping = value ?? throw new ArgumentNullException(nameof(ColumnMapping));
        }

        public IReadOnlyDictionary<string, object> Where
        {
            get => _where;
            set => _where = value ?? throw new ArgumentNullException(nameof(Where));
        }

        protected override ISetting ReadCore(IReadOnlyCollection<SoftString> names)
        {
            return SqlHelper.Execute(ConnectionString, connection =>
            {
                using (var command = connection.CreateSelectCommand(this, names))
                using (var settingReader = command.ExecuteReader())
                {
                    if (settingReader.Read())
                    {
                        var setting = new Setting((string)settingReader[ColumnMapping.Name])
                        {
                            Value = settingReader[ColumnMapping.Value],
                        };

                        if (settingReader.Read())
                        {
                            throw CreateAmbiguousSettingException(names);
                        }

                        return setting;
                    }

                    return null;
                }
            });
        }

        protected override void WriteCore(ISetting setting)
        {
            SqlHelper.Execute(ConnectionString, connection =>
            {
                using (var cmd = connection.CreateUpdateCommand(this, setting))
                {
                    //cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            });
        }
    }
}
