using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using SQLite;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// The code itself does not call some getters / setters explicitly,
// but they are used by sql-net-pcl and/or JsonSerializer/JsonDeserializer.

namespace Sufni.Bridge.Models;

[Table("linkage")]
public class Linkage
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Linkage() { }
    
    public Linkage(int? id, string name, double headAngle, double? frontStroke, double? rearStroke, string? rawData)
    {
        Id = id;
        Name = name;
        HeadAngle = headAngle;
        FrontStroke = frontStroke;
        RearStroke = rearStroke;
        RawData = rawData;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("head_angle")]
    [Column("head_angle")]
    public double HeadAngle { get; set; }

    [JsonPropertyName("front_stroke")]
    [Column("front_stroke")]
    public double? FrontStroke { get; set; }

    [JsonPropertyName("rear_stroke")]
    [Column("rear_stroke")]
    public double? RearStroke { get; set; }

    [JsonPropertyName("data")]
    [Column("raw_lr_data")]
    public string? RawData { get; set; }
    
    private LeverageRatioData? leverageRatioData;

    [JsonIgnore] [Ignore] public LeverageRatioData? LeverageRatioData 
    {
        get
        {
            if (leverageRatioData is null)
            {
                // Linkage.data does not have a header, it's just "wt,lr" pairs per line,
                // so we add the header so that LeverageFromCsv can process it.
                var csv = $"Wheel_T,Leverage_R\n{RawData}";
                leverageRatioData = LeverageFromCsv(new StringReader(csv));
            }

            return leverageRatioData;
        }
    }
    
    private static LeverageRatioData ProcessWheelLeverageRatio(IReader reader)
    {
        var wheelTravel = new List<double>();
        var leverageRatio = new List<double>();

        while (reader.Read())
        {
            var wheel = reader.GetField<double>("Wheel_T");
            var leverage = reader.GetField<double>("Leverage_R");
            
            wheelTravel.Add(wheel);
            leverageRatio.Add(leverage);
        }

        return new LeverageRatioData(wheelTravel, leverageRatio);
    }

    private static LeverageRatioData ProcessShockTravelLeverageRatio(IReader reader)
    {
        var shockTravel = new List<double>();
        var wheelTravel = new List<double>();
        var leverageRatio = new List<double>();
        var idx = 0;

        while (reader.Read())
        {
            var shock = reader.GetField<double>("Shock_T");
            var wheel = reader.GetField<double>("Wheel_T");
            double lr = 0;
            
            if (idx > 0)
            {
                var sdiff = shock - shockTravel[idx - 1];
                var wdiff = wheel - wheelTravel[idx - 1];
                lr = wdiff / sdiff;
                leverageRatio[idx - 1] = lr;
            }

            shockTravel.Add(shock);
            wheelTravel.Add(wheel);
            idx++;

            leverageRatio.Add(lr);
        }
        
        return new LeverageRatioData(wheelTravel, leverageRatio);
    }

    public static LeverageRatioData LeverageFromCsv(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            AllowComments = true,
            Comment = '#'
        };
        
        using var csvReader = new CsvReader(reader, config);

        if (!csvReader.Read() || !csvReader.ReadHeader() || !csvReader.HeaderRecord!.Contains("Wheel_T"))
        {
            throw new Exception("Failed processing leverage data.");
        }
        
        if (csvReader.HeaderRecord!.Contains("Leverage_R"))
        {
            return ProcessWheelLeverageRatio(csvReader);
        }

        if (csvReader.HeaderRecord!.Contains("Shock_T"))
        {
            return ProcessShockTravelLeverageRatio(csvReader);
        }

        throw new Exception("Failed processing leverage data.");
    }
}

public class LeverageRatioData
{
    public LeverageRatioData(List<double> wheelTravel, List<double> leverageRatio)
    {
        WheelTravel = wheelTravel;
        LeverageRatio = leverageRatio;
    }

    public override string ToString()
    {
        return string.Join("\n", WheelTravel.Zip(LeverageRatio, (wt, lr) => 
            string.Create(CultureInfo.InvariantCulture,$"{wt},{lr}")));
    }

    public override int GetHashCode()
    {
        return LeverageRatio.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is LeverageRatioData other)
        {
            return WheelTravel.SequenceEqual(other.WheelTravel) &&
                   LeverageRatio.SequenceEqual(other.LeverageRatio);
        }

        return false;
    }

    public List<double> WheelTravel { get; init; }
    public List<double> LeverageRatio { get; init; }
}