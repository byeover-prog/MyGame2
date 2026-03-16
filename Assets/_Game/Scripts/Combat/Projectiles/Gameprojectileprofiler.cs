// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/GameProjectileProfiler.cs
using Unity.Profiling;

/// <summary>투사체 시스템 ProfilerMarker 모음. Deep Profiling 없이도 구간 측정 가능.</summary>
public static class GameProjectileProfiler
{
    public static readonly ProfilerMarker Update       = new ProfilerMarker("GameProjectile.Update");
    public static readonly ProfilerMarker Simulate     = new ProfilerMarker("GameProjectile.Simulate");
    public static readonly ProfilerMarker Collision    = new ProfilerMarker("GameProjectile.Collision");
    public static readonly ProfilerMarker FlushSplits  = new ProfilerMarker("GameProjectile.FlushSplits");
    public static readonly ProfilerMarker SyncViews    = new ProfilerMarker("GameProjectile.SyncViews");
    public static readonly ProfilerMarker EmitVfx      = new ProfilerMarker("GameProjectile.EmitVfx");
}