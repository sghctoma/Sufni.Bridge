using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MessagePack;
using SQLite;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Services;

public class SqLiteDatabaseService : IDatabaseService
{
    private Task Initialization { get; }
    private readonly SQLiteAsyncConnection connection;
    
    private static readonly List<CalibrationMethod> DefaultCalibrationMethods = new()
    {
        new(1,
            "fraction", 
            "Sample is in fraction of maximum suspension stroke.",
            new CalibrationMethodProperties(
                new List<string>(),
                new Dictionary<string, string>(),
                "sample * MAX_STROKE")),
        new(2,
            "percentage", 
            "Sample is in percentage of maximum suspension stroke.",
            new CalibrationMethodProperties(
                new List<string>(),
                new Dictionary<string, string>()
                {
                    {"factor", "MAX_STROKE / 100.0"}
                },
                "sample * factor")),
        new(3,
            "linear", 
            "Sample is linearly distributed within a given range.",
            new CalibrationMethodProperties(
                new List<string>()
                {
                    "min_measurement",
                    "max_measurement"
                },
                new Dictionary<string, string>()
                {
                    {"factor", "MAX_STROKE / (max_measurement - min_measurement)"}
                },
                "(sample - min_measurement) * factor")),
        new(4,
            "as5600-isosceles-triangle", 
            "Triangle setup with the sensor between the base and leg.",
            new CalibrationMethodProperties(
                new List<string>()
                {
                    "arm",
                    "max"
                },
                new Dictionary<string, string>()
                {
                    {"start_angle", "acos(max / 2.0 / arm)"},
                    {"factor", "2.0 * pi / 4096"},
                    {"dbl_arm", "2.0 * arm"},
                },
                "max - (dbl_arm * cos((factor*sample) + start_angle))")),
        new(5,
            "as5600-triangle", 
            "Triangle setup with the sensor between two known sides.",
            new CalibrationMethodProperties(
                new List<string>()
                {
                    "arm1",
                    "arm2",
                    "max"
                },
                new Dictionary<string, string>()
                {
                    {"start_angle", "acos((arm1^2+arm2^2-max^2)/(2*arm1*arm2))"},
                    {"factor", "2.0 * pi / 4096"},
                    {"arms_sqr_sum", "arm1^2 + arm2^2"},
                    {"dbl_arm1_arm2", "2 * arm1 * arm2"},
                },
                "max - sqrt(arms_sqr_sum - dbl_arm1_arm2 * cos(start_angle-(factor*sample)))")),
    };
    
    public SqLiteDatabaseService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sst");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        connection = new SQLiteAsyncConnection(Path.Combine(dir, "sst.db"));
        Initialization = Init();
    }

    private async Task Init()
    {
        if (connection == null)
        {
            throw new Exception("Database connection failed!");
        }

        await connection.EnableWriteAheadLoggingAsync();
        var result = await connection.CreateTablesAsync(CreateFlags.None, new[]
        {
            typeof(Board),
            typeof(Linkage),
            typeof(CalibrationMethod),
            typeof(Calibration),
            typeof(Setup),
            typeof(Session)
        });

        if (result.Results[typeof(Board)] == CreateTableResult.Created)
        {
            var timestamp = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            foreach (var calibrationMethod in DefaultCalibrationMethods)
            {
                calibrationMethod.Updated = timestamp;
            }
            await connection.InsertAllAsync(DefaultCalibrationMethods);
        }
    }

    public async Task<List<Board>> GetBoardsAsync()
    {
        await Initialization;
        
        return await connection.Table<Board>().Where(b => b.Deleted == null).ToListAsync();
    }

    public async Task PutBoardAsync(Board board)
    {
        await Initialization;

        board.Updated = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        var existing = await connection.Table<Board>()
            .Where(b => b.Id == board.Id && b.Deleted == null)
            .FirstOrDefaultAsync() is not null;
        if (existing)
        {
            await connection.UpdateAsync(board);
        }
        else
        {
            await connection.InsertAsync(board);
        }
    }

    public async Task<List<Linkage>> GetLinkagesAsync()
    {
        await Initialization;
        
        return await connection.Table<Linkage>().Where(t => t.Deleted == null).ToListAsync();
    }
    
    public async Task<Linkage?> GetLinkageAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<Linkage>()
            .Where(l => l.Id == id && l.Deleted == null)
            .FirstOrDefaultAsync();
    }

    public async Task<int> PutLinkageAsync(Linkage linkage)
    {
        await Initialization;
        
        linkage.Updated = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        if (linkage.Id is not null)
        {
            await connection.UpdateAsync(linkage);
        }
        else
        {
            await connection.InsertAsync(linkage);
        }

        return linkage.Id ?? 0;
    }

    public async Task DeleteLinkageAsync(int id)
    {
        await Initialization;
        var linkage = await connection.Table<Linkage>()
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync();
        if (linkage is not null)
        {
            linkage.Deleted = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            await connection.UpdateAsync(linkage);
        }
    }

    public async Task<List<CalibrationMethod>> GetCalibrationMethodsAsync()
    {
        await Initialization;
        return await connection.Table<CalibrationMethod>().Where(c => c.Deleted == null).ToListAsync();
    }

    public async Task<CalibrationMethod?> GetCalibrationMethodAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<CalibrationMethod>()
            .Where(c => c.Id == id && c.Deleted == null)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<Calibration>> GetCalibrationsAsync()
    {
        await Initialization;
        return await connection.Table<Calibration>().Where(c => c.Deleted == null).ToListAsync();
    }
    
    public async Task<Calibration?> GetCalibrationAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<Calibration>()
            .Where(c => c.Id == id && c.Deleted == null)
            .FirstOrDefaultAsync();
    }

    public async Task<int> PutCalibrationAsync(Calibration calibration)
    {
        await Initialization;

        calibration.Updated = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        if (calibration.Id is not null)
        {
            await connection.UpdateAsync(calibration);
        }
        else
        {
            await connection.InsertAsync(calibration);
        }

        return calibration.Id ?? 0;
    }

    public async Task DeleteCalibrationAsync(int id)
    {
        await Initialization;
        var calibration = await connection.Table<Calibration>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();
        if (calibration is not null)
        {
            calibration.Deleted = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            await connection.UpdateAsync(calibration);
        }
    }

    public async Task<List<Setup>> GetSetupsAsync()
    {
        await Initialization;
        return await connection.Table<Setup>().Where(s => s.Deleted == null).ToListAsync();
    }
    
    public async Task<Setup?> GetSetupAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<Setup>()
            .Where(s => s.Id == id && s.Deleted == null)
            .FirstOrDefaultAsync();
    }

    public async Task<int> PutSetupAsync(Setup setup)
    {
        await Initialization;

        setup.Updated = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        if (setup.Id is not null)
        {
            await connection.UpdateAsync(setup);
        }
        else
        {
            await connection.InsertAsync(setup);
        }

        return setup.Id ?? 0;
    }

    public async Task DeleteSetupAsync(int id)
    {
        await Initialization;
        var setup = await connection.Table<Setup>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
        if (setup is not null)
        {
            setup.Deleted = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            await connection.UpdateAsync(setup);
        }
    }

    public async Task<List<Session>> GetSessionsAsync()
    {
        await Initialization;
        var sessions = await connection.QueryAsync<Session>(
            "SELECT id, name, setup_id, description, timestamp, track_id FROM session WHERE deleted IS null ORDER BY timestamp DESC");
        return sessions;
    }
    
    public async Task<TelemetryData?> GetSessionPsstAsync(int id)
    {
        await Initialization;
        var sessions = await connection.QueryAsync<Session>(
            "SELECT data FROM session WHERE deleted IS null AND id = ?", id);
        return sessions.Count == 1 ? MessagePackSerializer.Deserialize<TelemetryData>(sessions[0].ProcessedData) : null;
    }
    
    public async Task<byte[]?> GetSessionRawPsstAsync(int id)
    {
        await Initialization;
        var sessions = await connection.QueryAsync<Session>(
            "SELECT data FROM session WHERE deleted IS null AND id = ?", id);
        return sessions.Count == 1 ? sessions[0].ProcessedData : null;
    }

    public async Task<int> PutSessionAsync(Session session)
    {
        await Initialization;

        session.Updated = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        if (session.Id is not null)
        {
            await connection.UpdateAsync(session);
        }
        else
        {
            await connection.InsertAsync(session);
        }

        return session.Id ?? 0;
    }

    public async Task DeleteSessionAsync(int id)
    {
        await Initialization;
        var session = await connection.Table<Session>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
        if (session is not null)
        {
            session.Deleted = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            await connection.UpdateAsync(session);
        }
    }
}