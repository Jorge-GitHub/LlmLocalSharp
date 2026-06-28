using LlmLocalSharp.Core.Engines.Base;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Core.Entities.Settings.Engines;
using LlmLocalSharp.Engines.Providers.LlamaService.FactoryService;

namespace LlmLocalSharp.Core.Engines.Repositories;

internal class EngineRepository : IDisposable
{
    private readonly List<LlmEngineBase> _engines = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>Gets an existing compatible engine or creates and caches a new one.</summary>
    public LlmEngineBase GetEngine(LlmLocalClientSettings settings)
    {
        lock (this._lock)
        {
            LlmEngineBase? engine = this.GetCachedEngine(settings.Runtime);

            if (engine is null)
            {
                engine = this.CreateEngine(settings);
                this._engines.Add(engine);
            }

            return engine;
        }
    }

    /// <summary>Finds an engine already configured for the requested runtime.</summary>
    private LlmEngineBase? GetCachedEngine(LlmRuntimeSettings runtime)
    {
        if (this._engines.Count > 0)
        {
            foreach (LlmEngineBase engine in this._engines)
            {
                if (engine.Type == runtime.Engine
                    && engine.Backend == runtime.Backend
                    && engine.ModelName == runtime.ModelName)
                {
                    return engine;
                }
            }
        }

        return null;
    }

    /// <summary>Creates a concrete engine for the configured runtime.</summary>
    private LlmEngineBase CreateEngine(LlmLocalClientSettings settings)
    {
        return settings.Runtime.Engine switch
        {
            LlmEngineType.Llama => new LlamaEngineFactory()
            .Create(settings),

            _ => throw new NotImplementedException(
                    $"Engine '{settings.Runtime.Engine}' is not implemented yet.")
        };
    }

    /// <summary>Disposes all cached engines.</summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        foreach (LlmEngineBase engine in this._engines)
        {
            engine.Dispose();
        }
    }
}

