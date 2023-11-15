using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using Sufni.Bridge.Models;
using Calibration = Sufni.Bridge.Models.Calibration;
using Linkage = Sufni.Bridge.Models.Linkage;

namespace Sufni.Bridge.Services;

public class SqLiteDatabaseService : IDatabaseService
{
    private Task Initialization { get; }

    private readonly SQLiteAsyncConnection connection;
    
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
        
        await connection.CreateTableAsync<Board>();
        await connection.CreateTableAsync<Linkage>();
        await connection.CreateTableAsync<CalibrationMethod>();
        await connection.CreateTableAsync<Calibration>();
        await connection.CreateTableAsync<Setup>();
        await connection.CreateTableAsync<Session>();	
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

    public async Task<List<Calibration>> GetCalibrationsAsync()
    {
        await Initialization;
        return await connection.Table<Calibration>().ToListAsync();
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
        return await connection.Table<Session>().ToListAsync();
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