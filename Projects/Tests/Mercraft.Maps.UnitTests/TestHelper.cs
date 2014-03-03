﻿
using Mercraft.Core;

namespace Mercraft.Maps.UnitTests
{
    internal static class TestHelper
    {
        public static readonly GeoCoordinate BerlinGeoCenter = new GeoCoordinate(52.529814, 13.388015);

        public const string ConfigRootFile = "test.config";

        public const string TestPbfFilePath = @"..\..\..\..\Tests\TestAssets\kempen.osm.pbf";

        //52.53057 13.38687 52.52940 13.39022
        public const string TestXmlFilePath = @"..\..\..\..\Tests\TestAssets\berlin_house.osm.xml";


        public const string TestBigPbfFilePath = @"..\..\..\..\Tests\TestAssets\berlin-latest.osm.pbf";
    }
}
