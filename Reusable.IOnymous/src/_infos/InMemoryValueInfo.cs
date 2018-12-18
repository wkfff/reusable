﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Reusable.Exceptionizer;

namespace Reusable.IOnymous
{
    internal class InMemoryResourceInfo : ResourceInfo
    {
        [CanBeNull]
        private readonly byte[] _data;

        private readonly ResourceMetadata _metadata;

        [CanBeNull]
        private readonly IEnumerable<IResourceInfo> _files;

        public InMemoryResourceInfo([NotNull] UriString uri) : this(uri, new byte[0], ResourceMetadata.Empty)
        {
            ModifiedOn = DateTime.UtcNow;
        }

        public InMemoryResourceInfo([NotNull] UriString uri, byte[] data, ResourceMetadata metadata)
            : base(uri)
        {
            _data = data;
            _metadata = metadata;
        }

        public override bool Exists => _data?.Length > 0;

        public override long? Length => _data?.Length;

        public override DateTime? CreatedOn { get; }

        public override DateTime? ModifiedOn { get; }

        public override async Task CopyToAsync(Stream stream)
        {
            if (Exists)
            {
                await stream.WriteAsync(_data, 0, _data.Length);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override Task<object> DeserializeAsync(Type targetType)
        {
            if (!Exists)
            {
                throw new InvalidOperationException($"Cannot deserialize '{Uri}' because it doesn't exist.");
            }

            using (var memoryStream = new MemoryStream(_data))
            {
                return Task.FromResult(ResourceHelper.CreateObject(memoryStream, _metadata));
            }
        }
    }
}