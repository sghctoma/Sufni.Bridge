using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Adletec.Sonic;
using MessagePack;
using SQLite;

namespace Sufni.Bridge.Models.Telemetry;

[Table("calibration")]
[MessagePackObject(keyAsPropertyName: true)]
public class Calibration : Synchronizable
{
    private Func<Dictionary<string, double>, double>? evaluatorDelegate;
    private readonly Dictionary<string, double> evaluatorEnvironment = new();
    
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Calibration() { }
    
    public Calibration(Guid? id, string name, Guid methodId, Dictionary<string, double> inputs)
    {
        Id = id;
        Name = name;
        MethodId = methodId;
        Inputs = inputs;
    }

    [JsonPropertyName("id")]
    [PrimaryKey]
    [Column("id")]
    [IgnoreMember]
    public Guid? Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("method_id")]
    [Column("method_id")]
    public Guid MethodId { get; set; }
    
    [JsonPropertyName("inputs")]
    [Ignore]
    public Dictionary<string, double> Inputs { get; set; } = null!;

    [JsonIgnore]
    [Column("inputs")]
    [IgnoreMember]
    public string? InputsRaw
    {
        get => JsonSerializer.Serialize(Inputs);
        set
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            Inputs = JsonSerializer.Deserialize<Dictionary<string, double>>(value) ??
                     throw new InvalidOperationException();
        }
    }

    public void Prepare(CalibrationMethod method, double maxStroke, double maxTravel)
    {
        var expression = Regex.Replace(method.Properties.Expression, @"\s+", "");
        var evaluator = Evaluator.Create()
            .UseCulture(CultureInfo.InvariantCulture)
            .AddConstant("MAX_STROKE", maxStroke)
            .AddConstant("MAX_TRAVEL", maxTravel)
            .Build();
        evaluatorDelegate = evaluator.CreateDelegate(expression);
        
        // Set calibration variables (a.k.a. inputs)
        foreach (var input in Inputs)
        {
            evaluatorEnvironment[input.Key] = input.Value;
        }
        
        // Calculate intermediates
        foreach (var intermediate in method.Properties.Intermediates)
        {
            var exp = Regex.Replace(intermediate.Value, @"\s+", "");
            evaluatorEnvironment[intermediate.Key] = evaluator.Evaluate(exp, evaluatorEnvironment);
        }
    }

    public double Evaluate(double sample)
    {
        if (evaluatorDelegate is null)
        {
            return double.NaN;
        }

        evaluatorEnvironment["sample"] = sample;

        try
        {
            return evaluatorDelegate(evaluatorEnvironment);
        }
        catch (Exception)
        {
            return double.NaN;
        }
    }
}
