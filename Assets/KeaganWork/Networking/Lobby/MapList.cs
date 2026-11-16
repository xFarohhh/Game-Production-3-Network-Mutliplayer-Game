using System.Collections.Generic;
using UnityEngine;

public static class MapList
{
    public struct MapInfo
    {
        public string mapDisplayName;
        public string mapName;
    }

    public static readonly List<MapInfo> Maps = new List<MapInfo>
    {
        new MapInfo { mapDisplayName = "Abandoned Asylum", mapName = "SampleScene" }
    };
    public static MapInfo defaultMap => Maps[0];

    public static bool isAvailable(string mapName)
    {
        return Application.CanStreamedLevelBeLoaded(mapName);
    }
    public static string displayMapName(string name)
    {
        foreach(MapInfo maps in Maps)
        {
            if(maps.mapName == name)
            {
                return isAvailable(name) ? maps.mapName : ($"{maps.mapName} is not in the build list");
            }
        }
        return name;
    }

    public static string nextMap(string nextMap)
    {
        for(int i = 0; i < Maps.Count; i++)
        {
            if(Maps[i].mapName == nextMap)
            {
                return Maps[(i+1) % Maps.Count].mapName;
            }
           
        }
        return defaultMap.mapName;
    }
    public static string ResolveLoadable(string requestedLoadable)
    {
        if (!string.IsNullOrEmpty(requestedLoadable) && isAvailable(requestedLoadable)) return requestedLoadable;
        foreach(MapInfo maps in Maps)
        {
            if(isAvailable(maps.mapName)) return maps.mapName;
        }
        return defaultMap.mapName;
    }
}