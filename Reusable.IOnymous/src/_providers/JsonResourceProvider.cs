﻿using System;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reusable.Exceptionizer;
using Reusable.Extensions;
using Reusable.OneTo1;
using Reusable.OneTo1.Converters.Generic;

namespace Reusable.IOnymous
{
    /*
     *
     * This converter is build on top of the Reusable.Converters.
     * It initializes each json-converter on demand.
     * 
     */

    [PublicAPI]
    public class JsonResourceProvider : ResourceConverter
    {
        [NotNull] private ITypeConverter _converter;

        [NotNull] private JsonSerializerSettings _settings;

        [NotNull] private IImmutableSet<Type> _stringTypes;

        public JsonResourceProvider(IResourceProvider resourceProvider) : base(resourceProvider, typeof(string))
        {
            _converter = TypeConverter.Empty;
            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                Converters =
                {
                    new StringEnumConverter(),
                    new Reusable.Utilities.JsonNet.ColorConverter()
                }
            };
            _stringTypes = new[]
                {
                    typeof(string),
                    typeof(Enum),
                    typeof(TimeSpan),
                    typeof(DateTime),
                    typeof(Color)
                }
                .ToImmutableHashSet();
        }

        [NotNull]
        public JsonSerializerSettings Settings
        {
            get => _settings;
            set => _settings = value ?? throw new ArgumentNullException(nameof(Settings));
        }

        [NotNull, ItemNotNull]
        public IImmutableSet<Type> StringTypes
        {
            get => _stringTypes;
            set => _stringTypes = value ?? throw new ArgumentNullException(nameof(StringTypes));
        }

        public static Func<IResourceProvider, IResourceProvider> Factory() => dercorable => new JsonResourceProvider(dercorable);

        protected override async Task<IResourceInfo> GetAsyncInternal(UriString uri, ResourceMetadata metadata = null)
        {
            var info = await ResourceProvider.GetAsync(uri);
            if (info.Exists)
            {
                var value = await info.DeserializeAsync(typeof(string));
                return new JsonResourceInfo
                (
                    uri,
                    value,
                    targetType =>
                        _converter.CanConvert(typeof(string), targetType)
                            ? _converter
                            : _converter = _converter.Add(GetOrAddDeserializer(targetType))
                );
            }
            else
            {
                return info;
            }
        }

        protected override async Task<IResourceInfo> PutAsyncInternal(UriString uri, Stream stream, ResourceMetadata metadata = null)
        {
            var value = ResourceHelper.CreateObject(stream, metadata);

            if (SupportedTypes.Contains(value.GetType()))
            {
                var fromType = stream.GetType();
                var serialized = (string)GetOrAddSerializer(fromType).Convert(value, typeof(string));

                using (var streamReader = serialized.ToStreamReader())
                {
                    return await ResourceProvider.PutAsync(uri, streamReader.BaseStream);
                }
            }

            throw DynamicException.Create("UnsupportedType", $"{GetType().ToPrettyString()} does not support '{value.GetType().ToPrettyString()}'.");
        }

        #region Helpers

        private ITypeConverter GetOrAddDeserializer(Type toType)
        {
            if (_converter.CanConvert(typeof(string), toType))
            {
                return _converter;
            }

            var converter = CreateJsonConverter(typeof(JsonToObjectConverter<>), toType);
            return (_converter = _converter.Add(converter));
        }

        private ITypeConverter GetOrAddSerializer(Type fromType)
        {
            if (_converter.CanConvert(fromType, typeof(string)))
            {
                return _converter;
            }

            var converter = CreateJsonConverter(typeof(ObjectToJsonConverter<>), fromType);
            return (_converter = _converter.Add(converter));
        }

        private ITypeConverter CreateJsonConverter(Type converterType, Type valueType)
        {
            var converterGenericType = converterType.MakeGenericType(valueType);
            return (ITypeConverter)Activator.CreateInstance(converterGenericType, _settings, StringTypes);
        }

        #endregion
    }

    internal class JsonResourceInfo : ResourceInfo
    {
        [CanBeNull] private readonly object _value;

        private readonly Func<Type, ITypeConverter> _getOrAddConverter;

        internal JsonResourceInfo
        (
            [NotNull] UriString uri,
            [CanBeNull] object value,
            Func<Type, ITypeConverter> getOrAddConverter
        )
            : base(uri)
        {
            _value = value;
            _getOrAddConverter = getOrAddConverter;
        }

        public override bool Exists => !(_value is null);

        public override long? Length { get; }

        public override DateTime? CreatedOn { get; }

        public override DateTime? ModifiedOn { get; }

        protected override Task CopyToAsyncInternal(Stream stream)
        {
            // ReSharper disable once AssignNullToNotNullAttribute - this isn't null here
            new BinaryFormatter().Serialize(stream, _value);
            return Task.CompletedTask;
        }

        protected override Task<object> DeserializeAsyncInternal(Type targetType)
        {
            var converter = _getOrAddConverter(targetType);
            return Task.FromResult(converter.Convert(_value, targetType));
        }
    }
}