using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MessagePack;
using SQLite;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;
using Calibration = Sufni.Bridge.Models.Calibration;
using Linkage = Sufni.Bridge.Models.Linkage;

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
                    {"start_angle", "acos((arm1**2+arm2**2-max**2)/(2*arm1*arm2))"},
                    {"factor", "2.0 * pi / 4096"},
                    {"arms_sqr_sum", "arm1**2 + arm2**2"},
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
            await connection.InsertAllAsync(DefaultCalibrationMethods);
        }
    }

    public async Task<List<Board>> GetBoardsAsync()
    {
        await Initialization;
        
        return await connection.Table<Board>().ToListAsync();
    }

    public async Task PutBoardAsync(Board board)
    {
        await Initialization;

        var existing = await connection.Table<Board>()
            .Where(b => b.Id == board.Id)
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
        
        return await connection.Table<Linkage>().ToListAsync();
    }
    
    public async Task<Linkage?> GetLinkageAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<Linkage>()
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> PutLinkageAsync(Linkage linkage)
    {
        await Initialization;
        
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
        await connection.DeleteAsync<Linkage>(id);
    }

    public async Task<List<CalibrationMethod>> GetCalibrationMethodsAsync()
    {
        await Initialization;
        return await connection.Table<CalibrationMethod>().ToListAsync();
    }

    public async Task<CalibrationMethod?> GetCalibrationMethodAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<CalibrationMethod>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<Calibration>> GetCalibrationsAsync()
    {
        await Initialization;
        return await connection.Table<Calibration>().ToListAsync();
    }
    
    public async Task<Calibration?> GetCalibrationAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<Calibration>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> PutCalibrationAsync(Calibration calibration)
    {
        await Initialization;

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
        await connection.DeleteAsync<Calibration>(id);
    }

    public async Task<List<Setup>> GetSetupsAsync()
    {
        await Initialization;
        return await connection.Table<Setup>().ToListAsync();
    }
    
    public async Task<Setup?> GetSetupAsync(int id)
    {
        await Initialization;
        
        return await connection.Table<Setup>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> PutSetupAsync(Setup setup)
    {
        await Initialization;

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
        await connection.DeleteAsync<Setup>(id);
    }

    public async Task<List<Session>> GetSessionsAsync()
    {
        await Initialization;
        var sessions = await connection.QueryAsync<Session>(
            "SELECT id, name, setup_id, description, timestamp, track_id FROM session ORDER BY timestamp DESC");
        return sessions;
    }
    
    public async Task<TelemetryData?> GetSessionPsstAsync(int id)
    {
        await Initialization;
        var sessions = await connection.QueryAsync<Session>(
            "SELECT data FROM session WHERE id = ?", id);
        return sessions.Count == 1 ? MessagePackSerializer.Deserialize<TelemetryData>(sessions[0].ProcessedData) : null;
    }

    public async Task<int> PutSessionAsync(Session session)
    {
        await Initialization;

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
        await connection.DeleteAsync<Session>(id);
    }
}