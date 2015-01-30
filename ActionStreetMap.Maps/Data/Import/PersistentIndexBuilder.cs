﻿using System;
using System.IO;
using System.Linq;
using ActionStreetMap.Core;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.IO;
using ActionStreetMap.Maps.Data.Spatial;
using ActionStreetMap.Maps.Data.Storage;
using ActionStreetMap.Maps.Formats;

namespace ActionStreetMap.Maps.Data.Import
{
    internal class PersistentIndexBuilder : IndexBuilder
    {
        private readonly string _filePath;
        private readonly string _outputDirectory;
        private readonly IFileSystemService _fileSystemService;

        public PersistentIndexBuilder(string filePath, string outputDirectory, IFileSystemService fileSystemService,
            IndexSettings settings, ITrace trace)
            : base(settings, trace)
        {
            _filePath = filePath;
            _outputDirectory = outputDirectory;
            _fileSystemService = fileSystemService;
        }

        public override void Build()
        {
            var sourceStream = _fileSystemService.ReadStream(_filePath);
            var format = _filePath.Split('.').Last();
            var reader = GetReader(format);

            var kvUsageMemoryStream = new MemoryStream();
            var kvUsage = new KeyValueUsage(kvUsageMemoryStream);

            var keyValueStoreFile = _fileSystemService.WriteStream(String.Format(Consts.KeyValueStorePathFormat, _outputDirectory));
            var index = new KeyValueIndex(Settings.Search.KvIndexCapacity, Settings.Search.PrefixLength);
            var keyValueStore = new KeyValueStore(index, kvUsage, keyValueStoreFile);

            var storeFile = _fileSystemService.WriteStream(String.Format(Consts.ElementStorePathFormat, _outputDirectory));
            Store = new ElementStore(keyValueStore, storeFile);
            Tree = new RTree<uint>(65);

            reader.Read(new ReaderContext
            {
                SourceStream = sourceStream,
                Builder = this,
                ReuseEntities = false,
                SkipTags = false,
            });

            Clear();
            Complete();

            using (var kvFileStream = _fileSystemService.WriteStream(String.Format(Consts.KeyValueUsagePathFormat, _outputDirectory)))
            {
                var buffer = kvUsageMemoryStream.GetBuffer();
                kvFileStream.Write(buffer, 0, (int) kvUsageMemoryStream.Length);
            }

            KeyValueIndex.Save(index, _fileSystemService.WriteStream(String.Format(Consts.KeyValueIndexPathFormat, _outputDirectory)));
            SpatialIndex<uint>.Save(Tree, _fileSystemService.WriteStream(String.Format(Consts.SpatialIndexPathFormat, _outputDirectory)));
            Store.Dispose();
        }

        public override void ProcessBoundingBox(BoundingBox bbox)
        {
            using (var writer = new StreamWriter(_fileSystemService.WriteStream(String.Format(Consts.HeaderPathFormat, _outputDirectory))))
            {
                writer.Write("{0} {1}", bbox.MinPoint, bbox.MaxPoint);
            }
        }
    }
}