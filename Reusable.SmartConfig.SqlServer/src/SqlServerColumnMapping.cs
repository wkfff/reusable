﻿using JetBrains.Annotations;
using Reusable.Validation;

namespace Reusable.SmartConfig
{
    public class SqlServerColumnMapping
    {
        private static readonly IWeelidator<string> ColumnValidator = new Weelidator<string>(builder =>
        {
            builder.BlockNull();
            builder.Block(c => string.IsNullOrEmpty(c));
        });

        private string _name;
        private string _value;

        public SqlServerColumnMapping()
        {
            Name = nameof(Name);
            Value = nameof(Value);
        }

        [NotNull]
        public string Name
        {
            get => _name;
            set => _name = value.ValidateWith(ColumnValidator).ThrowIfInvalid();
        }

        [NotNull]
        public string Value
        {
            get => _value;
            set => _value = value.ValidateWith(ColumnValidator).ThrowIfInvalid();
        }

        public static implicit operator SqlServerColumnMapping((string name, string value) mapping)
        {
            return new SqlServerColumnMapping
            {
                Name = mapping.name,
                Value = mapping.value
            };
        }
    }
}