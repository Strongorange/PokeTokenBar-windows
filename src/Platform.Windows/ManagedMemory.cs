using System.Runtime;

namespace PokeTokenBar.Platform.Windows;

public static class ManagedMemory
{
    /// <summary>
    /// Compacts every generation, the large object heap included, and returns the freed pages to Windows.
    /// </summary>
    public static void ReturnFreedMemory()
    {
        // 기본 GC 는 LOH 를 압축하지 않아서, 갱신이 버린 큰 배열 자리가 Private 에 계속 남음
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
    }
}
