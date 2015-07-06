using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatCanet
{
    public struct Biome
    {
        public readonly BiomeType MainBiome;

        public readonly SubBiomeType SubBiome;

        public Biome(SubBiomeType micro)
        {
            MainBiome = Biome.Generalise(micro);
            SubBiome = micro;
        }

        public override string ToString()
        {
            string main = String.Concat(MainBiome.ToString().Select(c => Char.IsUpper(c) ? " " + c : c.ToString())).TrimStart(' ');
            string sub = String.Concat(SubBiome.ToString().Select(c => Char.IsUpper(c) ? " " + c : c.ToString())).TrimStart(' ');
            return String.Format("{0} ({1})", main, sub);
        }

        private static BiomeType Generalise(SubBiomeType b)
        {
            switch (b)
            {
                case SubBiomeType.Ocean:
                    return BiomeType.Ocean;
                case SubBiomeType.Mountain:
                    return BiomeType.Mountain;
                case SubBiomeType.Beach:
                    return BiomeType.Beach;

                case SubBiomeType.PolarDesert:
                case SubBiomeType.IceField:
                    return BiomeType.Arctic;

                case SubBiomeType.SubpolarMoistTundra:
                case SubBiomeType.SubpolarWetTundra:
                case SubBiomeType.SubpolarRainTundra:
                    return BiomeType.Tundra;

                case SubBiomeType.BorealDesert:
                case SubBiomeType.BorealDryScrub:
                case SubBiomeType.SubpolarDryTundra:
                    return BiomeType.ColdDesert;

                case SubBiomeType.BorealMoistForest:
                case SubBiomeType.BorealWetForest:
                case SubBiomeType.BorealRainforest:
                    return BiomeType.BorealForest;

                case SubBiomeType.CoolTemperateDesert:
                case SubBiomeType.CoolTemperateDesertScrub:
                case SubBiomeType.CoolTemperateSteppe:
                    return BiomeType.Steppe;
                     
                case SubBiomeType.CoolTemperateMoistForest:
                case SubBiomeType.CoolTemperateWetForest:
                case SubBiomeType.CoolTemperateRainforest:
                    return BiomeType.CoolTemperateForest;

                case SubBiomeType.WarmTemperateDryForest:
                case SubBiomeType.WarmTemperateMoistForest:
                case SubBiomeType.WarmTemperateWetForest:
                case SubBiomeType.WarmTemperateRainforest:
                    return BiomeType.WarmTemperateForest;

                case SubBiomeType.WarmTemperateDesert:
                case SubBiomeType.WarmTemperateDesertScrub:
                case SubBiomeType.SubtropicalDesert:
                case SubBiomeType.SubtropicalDesertScrub:
                case SubBiomeType.TropicalDesert:
                case SubBiomeType.TropicalDesertScrub:
                    return BiomeType.Desert;

                case SubBiomeType.WarmTemperateThornScrub:
                case SubBiomeType.SubtropicalThornWoodland:
                case SubBiomeType.TropicalThornWoodland:
                case SubBiomeType.TropicalVeryDryForest:
                    return BiomeType.Savanna;

                case SubBiomeType.SubtropicalDryForest:
                case SubBiomeType.TropicalDryForest:
                    return BiomeType.TropicalDryForest;

                case SubBiomeType.SubtropicalMoistForest:
                case SubBiomeType.SubtropicalWetForest:
                case SubBiomeType.SubtropicalRainforest:
                case SubBiomeType.TropicalMoistForest:
                case SubBiomeType.TropicalWetForest:
                case SubBiomeType.TropicalRainforest:
                    return BiomeType.Rainforest;
                default:
                    return BiomeType.Ocean;
            }
        }
    }

    public enum BiomeType
    {
        Ocean,
        Lake,
        River,
        Beach,
        Mountain,
        BorealForest,
        CoolTemperateForest,
        WarmTemperateForest,
        TropicalDryForest,
        Tundra,
        Arctic,
        Rainforest,
        Savanna,
        Desert,
        ColdDesert,
        Steppe,
        Heathland
    }

    public enum SubBiomeType
    {
        Ocean,
        Mountain,
        Beach,
        PolarDesert,
        IceField,
        SubpolarDryTundra,
        SubpolarMoistTundra,
        SubpolarWetTundra,
        SubpolarRainTundra,
        BorealDesert,
        BorealDryScrub,
        BorealMoistForest,
        BorealWetForest,
        BorealRainforest,
        CoolTemperateDesert,
        CoolTemperateDesertScrub,
        CoolTemperateSteppe,
        CoolTemperateMoistForest,
        CoolTemperateWetForest,
        CoolTemperateRainforest,
        WarmTemperateDesert,
        WarmTemperateDesertScrub,
        WarmTemperateThornScrub,
        WarmTemperateDryForest,
        WarmTemperateMoistForest,
        WarmTemperateWetForest,
        WarmTemperateRainforest,
        SubtropicalDesert,
        SubtropicalDesertScrub,
        SubtropicalThornWoodland,
        SubtropicalDryForest,
        SubtropicalMoistForest,
        SubtropicalWetForest,
        SubtropicalRainforest,
        TropicalDesert,
        TropicalDesertScrub,
        TropicalThornWoodland,
        TropicalVeryDryForest,
        TropicalDryForest,
        TropicalMoistForest,
        TropicalWetForest,
        TropicalRainforest
    }

    public enum Temperature
    {
        Polar = 0,
        Subpolar,
        Boreal,
        Cool,
        Warm,
        Subtropical,
        Tropical
    }

    public enum Moisture
    {
        Superarid = 0,
        Perarid,
        Arid,
        Semiarid,
        Subhumid,
        Humid,
        Perhumid,
        Superhumid
    }

    public static class Biomes
    {
        //heavily taken from worldengine
        public static Biome Classify(int t, int m)
        {
            Moisture moisture = (Moisture)m;
            Temperature temperature = (Temperature)t;
            SubBiomeType subType = SubBiomeType.Ocean;
            switch (temperature)
            {
                case Temperature.Polar:
                    subType = moisture == Moisture.Superarid ? SubBiomeType.PolarDesert : SubBiomeType.IceField;
                    break;
                case Temperature.Subpolar:
                    if (moisture == Moisture.Superarid)
                        subType = SubBiomeType.SubpolarDryTundra;
                    else if (moisture == Moisture.Perarid)
                        subType = SubBiomeType.SubpolarMoistTundra;
                    else if (moisture == Moisture.Arid)
                        subType = SubBiomeType.SubpolarWetTundra;
                    else
                        subType = SubBiomeType.SubpolarRainTundra;
                    break;
                case Temperature.Boreal:
                    if (moisture == Moisture.Superarid)
                        subType = SubBiomeType.BorealDesert;
                    else if (moisture == Moisture.Perarid)
                        subType = SubBiomeType.BorealDryScrub;
                    else if (moisture == Moisture.Arid)
                        subType = SubBiomeType.BorealMoistForest;
                    else if (moisture == Moisture.Semiarid)
                        subType = SubBiomeType.BorealWetForest;
                    else
                        subType = SubBiomeType.BorealRainforest;
                    break;
                case Temperature.Cool:
                    if (moisture == Moisture.Superarid)
                        subType = SubBiomeType.CoolTemperateDesert;
                    else if (moisture == Moisture.Perarid)
                        subType = SubBiomeType.CoolTemperateDesertScrub;
                    else if (moisture == Moisture.Arid)
                        subType = SubBiomeType.CoolTemperateSteppe;
                    else if (moisture == Moisture.Semiarid)
                        subType = SubBiomeType.CoolTemperateMoistForest;
                    else if (moisture == Moisture.Subhumid)
                        subType = SubBiomeType.CoolTemperateWetForest;
                    else
                        subType = SubBiomeType.CoolTemperateRainforest;
                    break;
                case Temperature.Warm:
                    if (moisture == Moisture.Superarid)
                        subType = SubBiomeType.WarmTemperateDesert;
                    else if (moisture == Moisture.Perarid)
                        subType = SubBiomeType.WarmTemperateDesertScrub;
                    else if (moisture == Moisture.Arid)
                        subType = SubBiomeType.WarmTemperateThornScrub;
                    else if (moisture == Moisture.Semiarid)
                        subType = SubBiomeType.WarmTemperateDryForest;
                    else if (moisture == Moisture.Subhumid)
                        subType = SubBiomeType.WarmTemperateMoistForest;
                    else if (moisture == Moisture.Humid)
                        subType = SubBiomeType.WarmTemperateWetForest;
                    else
                        subType = SubBiomeType.WarmTemperateRainforest;
                    break;
                case Temperature.Subtropical:
                    if (moisture == Moisture.Superarid)
                        subType = SubBiomeType.SubtropicalDesert;
                    else if (moisture == Moisture.Perarid)
                        subType = SubBiomeType.SubtropicalDesertScrub;
                    else if (moisture == Moisture.Arid)
                        subType = SubBiomeType.SubtropicalThornWoodland;
                    else if (moisture == Moisture.Semiarid)
                        subType = SubBiomeType.SubtropicalDryForest;
                    else if (moisture == Moisture.Subhumid)
                        subType = SubBiomeType.SubtropicalMoistForest;
                    else if (moisture == Moisture.Humid)
                        subType = SubBiomeType.SubtropicalWetForest;
                    else
                        subType = SubBiomeType.WarmTemperateRainforest;
                    break;
                case Temperature.Tropical:
                    if (moisture == Moisture.Superarid)
                        subType = SubBiomeType.TropicalDesert;
                    else if (moisture == Moisture.Perarid)
                        subType = SubBiomeType.TropicalDesertScrub;
                    else if (moisture == Moisture.Arid)
                        subType = SubBiomeType.TropicalThornWoodland;
                    else if (moisture == Moisture.Semiarid)
                        subType = SubBiomeType.TropicalVeryDryForest;
                    else if (moisture == Moisture.Subhumid)
                        subType = SubBiomeType.TropicalDryForest;
                    else if (moisture == Moisture.Humid)
                        subType = SubBiomeType.TropicalMoistForest;
                    else if (moisture == Moisture.Perhumid)
                        subType = SubBiomeType.TropicalMoistForest;
                    else
                        subType = SubBiomeType.TropicalRainforest;
                    break;
                default:
                    break;
            }
            return new Biome(subType);
        }
    }
}
