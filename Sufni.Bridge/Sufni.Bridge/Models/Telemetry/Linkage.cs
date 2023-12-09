using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using MessagePack;
using SQLite;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// The code itself does not call some getters / setters explicitly,
// but they are used by sql-net-pcl and/or JsonSerializer/JsonDeserializer.

namespace Sufni.Bridge.Models.Telemetry;

[Table("linkage")]
[MessagePackObject(keyAsPropertyName: true)]
public class Linkage
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Linkage() { }
    
    public Linkage(int? id, string name, double headAngle, double? maxFrontStroke, double? maxRearStroke, string? rawData)
    {
        Id = id;
        Name = name;
        HeadAngle = headAngle;
        MaxFrontStroke = maxFrontStroke;
        MaxRearStroke = maxRearStroke;
        RawData = rawData;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    [IgnoreMember]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("head_angle")]
    [Column("head_angle")]
    public double HeadAngle { get; set; }

    [JsonPropertyName("front_stroke")]
    [Column("front_stroke")]
    public double? MaxFrontStroke { get; set; }

    [JsonPropertyName("rear_stroke")]
    [Column("rear_stroke")]
    public double? MaxRearStroke { get; set; }
    
    [JsonPropertyName("data")]
    [Column("raw_lr_data")]
    [IgnoreMember]
    public string? RawData { get; set; }
    
    [Ignore] [JsonIgnore] public double MaxFrontTravel { get; set; }
    [Ignore] [JsonIgnore] public double MaxRearTravel { get; set; }
    [Ignore] [JsonIgnore] public double[]? ShockWheelCoeffs { get; set; }
    [Ignore] [JsonIgnore] public double[][]? LeverageRatio { get; set; }
    
    private LeverageRatioData? leverageRatioData;

    [Ignore] [JsonIgnore] [IgnoreMember] public LeverageRatioData? LeverageRatioData 
    {
        get
        {
            if (leverageRatioData is null && RawData is not null)
            {
                // Linkage.data does not have a header, it's just "wt,lr" pairs per line,
                // so we add the header so that LeverageFromCsv can process it.
                var csv = $"Wheel_T,Leverage_R\n{RawData}";
                leverageRatioData = new LeverageRatioData(new StringReader(csv));
            }

            if (leverageRatioData is null && LeverageRatio is not null)
            {
                leverageRatioData = new LeverageRatioData(LeverageRatio);
            }

            return leverageRatioData;
        }
    }
}

public class LeverageRatioData
{
    public List<double> WheelTravel { get; init; }
    public List<double> LeverageRatio { get; init; }
    
    private void ProcessWheelLeverageRatio(IReader reader)
    {
        while (reader.Read())
        {
            var wheel = reader.GetField<double>("Wheel_T");
            var leverage = reader.GetField<double>("Leverage_R");
            
            WheelTravel.Add(wheel);
            LeverageRatio.Add(leverage);
        }
    }

    private void ProcessShockTravelLeverageRatio(IReader reader)
    {
        var shockTravel = new List<double>();
        var idx = 0;

        while (reader.Read())
        {
            var shock = reader.GetField<double>("Shock_T");
            var wheel = reader.GetField<double>("Wheel_T");
            double lr = 0;
            
            if (idx > 0)
            {
                var sdiff = shock - shockTravel[idx - 1];
                var wdiff = wheel - WheelTravel[idx - 1];
                lr = wdiff / sdiff;
                LeverageRatio[idx - 1] = lr;
            }

            shockTravel.Add(shock);
            WheelTravel.Add(wheel);
            idx++;

            LeverageRatio.Add(lr);
        }
    }
    
    public LeverageRatioData(TextReader reader)
    {
        WheelTravel = new List<double>();
        LeverageRatio = new List<double>();
        
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
            ProcessWheelLeverageRatio(csvReader);
        }

        if (csvReader.HeaderRecord!.Contains("Shock_T"))
        {
            ProcessShockTravelLeverageRatio(csvReader);
        }
    }

    public LeverageRatioData(double[][] data)
    {
        WheelTravel = new List<double>(data.Length);
        LeverageRatio = new List<double>(data.Length);
        
        foreach (var d in data)
        {
            WheelTravel.Add(d[0]);
            LeverageRatio.Add(d[1]);
        }
    }

    public double[][] ToArray()
    {
        var data = new double[WheelTravel.Count][];
        for (var i = 0; i < WheelTravel.Count; i++)
        {
            data[i][0] = WheelTravel[i];
            data[i][1] = LeverageRatio[i];
        }

        return data;
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
}