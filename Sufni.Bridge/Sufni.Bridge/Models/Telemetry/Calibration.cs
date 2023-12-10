using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;
using NCalc;
using SQLite;

namespace Sufni.Bridge.Models.Telemetry;

[Table("calibration")]
[MessagePackObject(keyAsPropertyName: true)]
public class Calibration
{
    private Expression? expression;
    
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Calibration() { }
    
    public Calibration(int? id, string name, int methodId, Dictionary<string, double> inputs)
    {
        Id = id;
        Name = name;
        MethodId = methodId;
        Inputs = inputs;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    [IgnoreMember]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("method_id")]
    [Column("method_id")]
    public int MethodId { get; set; }
    
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
        expression = new Expression(method.Properties.Expression, EvaluateOptions.IgnoreCase);
        
        // Set calibration variables (a.k.a. inputs)
        foreach (var input in Inputs)
        {
            expression.Parameters[input.Key] = input.Value;
        }

        // Set standard variables
        expression.Parameters["MAX_STROKE"] = maxStroke;
        expression.Parameters["MAX_TRAVEL"] = maxTravel;
        expression.Parameters["pi"] = Math.PI;
        
        // Calculate intermediates
        foreach (var intermediate in method.Properties.Intermediates)
        {
            var exp = new Expression(intermediate.Value, EvaluateOptions.IgnoreCase)
            {
                Parameters = expression.Parameters
            };

            expression.Parameters[intermediate.Key] = (double)exp.Evaluate();
        }
    }

    public double Evaluate(double sample)
    {
        if (expression is null)
        {
            return double.NaN;
        }
        
        expression.Parameters["sample"] = sample;

        try
        {
            return (double)expression.Evaluate();
        }
        catch (Exception)
        {
            return double.NaN;
        }
    }
}
