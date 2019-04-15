using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flawless;
using Reusable.IOnymous;
using Reusable.OneTo1;
using Reusable.SmartConfig.Reflection;

namespace Reusable.SmartConfig
{
    internal interface IConfiguration<T>
    {
        Task<TValue> GetItemAsync<TValue>(Expression<Func<T, TValue>> getItem, string key = default);

        Task SetItemAsync<TValue>(Expression<Func<T, TValue>> setItem, TValue newValue, string key = default);
    }

    internal class Configuration<T> : IConfiguration<T>
    {
        //private static readonly IImmutableSet<MimeType> SupportedTypes = ImmutableHashSet<MimeType>.Empty.Add(MimeType.Text).Add(MimeType.Json);

        //public delegate IConfiguration<T> Factory();

        private readonly IResourceProvider _settingProvider;

        private ITypeConverter _converter;

        public Configuration([NotNull] IResourceProvider settingProvider)
        {
            _settingProvider = settingProvider ?? throw new ArgumentNullException(nameof(settingProvider));
            _converter = new JsonSettingConverter();
        }

        [NotNull]
        public ITypeConverter Converter
        {
            get => _converter;
            set => _converter = value ?? throw new ArgumentNullException(nameof(Converter));
        }

        public static IConfiguration Create(params IResourceProvider[] resourceProviders)
        {
            return new Configuration(new CompositeProvider(resourceProviders));
        }

        public async Task<TValue> GetItemAsync<TValue>(Expression<Func<T, TValue>> getItem, string key = default)
        {
            var settingMetadata = MemberMetadata.FromExpression(getItem, false);
            var uri = settingMetadata.CreateUri(key);
            var setting = await _settingProvider.GetAsync(uri, PopulateProviderInfo(settingMetadata));

            if (setting.Exists)
            {
                //if (!SupportedTypes.Contains(setting.Format))
                {
                    //throw DynamicException.Create("UnsupportedSettingFormat", $"'{setting.Format}' is not supported.");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await setting.CopyToAsync(memoryStream);
                    using (var streamReader = new StreamReader(memoryStream.Rewind()))
                    {
                        var data = await streamReader.ReadToEndAsync();
                        return (TValue)_converter.Convert(data, settingMetadata.MemberType);
                    }
                }
            }
            else
            {
                throw DynamicException.Create("SettingNotFound", $"Could not find '{uri}'.");
            }
        }

        public async Task SetItemAsync<TValue>(Expression<Func<T, TValue>> setItem, TValue newValue, string key = default)
        {
            var settingMetadata = MemberMetadata.FromExpression(setItem, false);
            var uri = settingMetadata.CreateUri(key);

            Validate(newValue, settingMetadata.Validations, uri);
            var data = (string)_converter.Convert(newValue, typeof(string));
            using (var stream = await ResourceHelper.SerializeAsTextAsync(data))
            {
                await _settingProvider.PutAsync(uri, stream, PopulateProviderInfo(settingMetadata, ResourceMetadata.Empty.Format(MimeType.Text)));
            }
        }

        #region Helpers

        private static object Validate(object value, IEnumerable<ValidationAttribute> validations, UriString uri)
        {
            foreach (var validation in validations)
            {
                validation.Validate(value, uri);
            }

            return value;
        }

        private static ResourceMetadata PopulateProviderInfo(MemberMetadata settingMetadata, ResourceMetadata metadata = default)
        {
            return
                metadata
                    .ProviderCustomName(settingMetadata.ResourceProviderName)
                    .ProviderDefaultName(settingMetadata.ResourceProviderType?.ToPrettyString());
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public class ResourcePrefixAttribute : Attribute
    {
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public class ResourceSchemeAttribute : Attribute
    {
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public class ResourceNameAttribute : Attribute
    {
        public string Name { get; }

        public SettingNameConvention? Convention { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public class ResourceProviderAttribute : Attribute
    {
        public string Name { get; }
        public Type Type { get; }
    }

    [PublicAPI]
    public class MemberMetadata
    {
        public static readonly IImmutableList<SettingProviderAttribute> AssemblyAttributes =
            AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetCustomAttributes<SettingProviderAttribute>())
                // Attributes with provider names have a higher specificity and should be matched first.
                .OrderByDescending(x => x.ProviderNameCount)
                //.Append(SettingProviderAttribute.Default) // In case there are not assembly-level attributes.
                .ToImmutableList();

        private static readonly IExpressValidator<LambdaExpression> SettingExpressionValidator = ExpressValidator.For<LambdaExpression>(builder =>
        {
            builder.NotNull();
            builder.True(e => e.Body is MemberExpression);
        });

        private MemberMetadata(Type type, object typeInstance, MemberInfo member)
        {
            Namespace = type.Namespace;
            Type = type;
            TypeName =
                GetCustomAttribute<ResourceNameAttribute>(type, default)?.Name ??
                (
                    type.IsInterface
                        ? Regex.Match(type.Name, @"^I(?<name>\w+)(?:Config(uration)?)", RegexOptions.IgnoreCase).Group("name")
                        : type.Name
                );
            TypeInstance = typeInstance;
            Member = member;
            MemberName = GetCustomAttribute<ResourceNameAttribute>(default, member)?.Name ?? member.Name;
            MemberType = GetMemberType(member);

            ResourceScheme = GetCustomAttribute<ResourceSchemeAttribute>(type, member)?.Name ?? "setting";
            ResourcePrefix = GetCustomAttribute<ResourceNameAttribute>(type, member)?.Name;
            ResourceProviderType = GetCustomAttribute<ResourceProviderAttribute>(type, member)?.Type;
            ResourceProviderName = GetCustomAttribute<ResourceProviderAttribute>(type, member)?.Name;
            Validations = member.GetCustomAttributes<ValidationAttribute>();
            ;
            DefaultValue = member.GetCustomAttribute<DefaultValueAttribute>()?.Value;
            ;
            Convention = GetCustomAttribute<ResourceNameAttribute>(type, member)?.Convention ?? SettingNameConvention.TypeMember;
        }

        [NotNull]
        public string Namespace { get; }

        [NotNull]
        public Type Type { get; }

        [CanBeNull]
        public string TypeName { get; }

        [CanBeNull]
        public object TypeInstance { get; }

        [NotNull]
        public MemberInfo Member { get; }

        [NotNull]
        public string MemberName { get; }

        [NotNull]
        public Type MemberType { get; }

        [NotNull]
        public string ResourceScheme { get; }

        [CanBeNull]
        public string ResourcePrefix { get; }

        [CanBeNull]
        public string ResourceProviderName { get; }

        [CanBeNull]
        public Type ResourceProviderType { get; }

        [NotNull, ItemNotNull]
        public IEnumerable<ValidationAttribute> Validations { get; }

        [CanBeNull]
        public object DefaultValue { get; }

        public SettingNameConvention Convention { get; }

        [NotNull]
        public static MemberMetadata FromExpression(LambdaExpression expression, bool nonPublic = false)
        {
            expression.ValidateWith(SettingExpressionValidator).Assert();

            var (type, instance, member) = SettingVisitor.GetSettingInfo(expression, nonPublic);
            return new MemberMetadata(type, instance, member);
        }

        public UriString CreateUri(string instanceName = null)
        {
            var query = (SoftString)new (SoftString Key, SoftString Value)[]
                {
                    ("prefix", ResourcePrefix),
                    ("instanceName", instanceName),
                    ("convention", Convention.ToString()),
                    //("providerCustomName", ProviderName),
                    //("providerDefaultName", ProviderType?.ToPrettyString())
                }
                .Where(x => x.Value)
                .Select(x => $"{x.Key.ToString()}={x.Value.ToString()}")
                .Join("&");

            return $"{ResourceScheme}:///{Namespace.Replace('.', '/')}/{TypeName}/{MemberName}{(query ? $"?{query.ToString()}" : string.Empty)}";
        }

        [NotNull]
        private Type GetMemberType(MemberInfo member)
        {
            switch (member)
            {
                case PropertyInfo property:
                    return property.PropertyType;

                case FieldInfo field:
                    return field.FieldType;

                default:
                    throw new ArgumentException($"Member must be either a {nameof(MemberTypes.Property)} or a {nameof(MemberTypes.Field)}.");
            }
        }

        [CanBeNull]
        private static T GetCustomAttribute<T>(Type type, MemberInfo member) where T : Attribute
        {
            return new[]
            {
                member?.GetCustomAttributes<T>(inherit: true).FirstOrDefault(),
                type?.GetCustomAttribute<T>(),
            }.FirstOrDefault(Conditional.IsNotNull);
        }
    }

    public enum SettingNameConvention
    {
        Inherit = -1,

        /// <summary>
        /// Member
        /// </summary>
        Member = 0,

        /// <summary>
        /// Type.Member
        /// </summary>
        TypeMember = 1,

        /// <summary>
        /// Namespace+Type.Member
        /// </summary>
        NamespaceTypeMember = 2,
    }
}