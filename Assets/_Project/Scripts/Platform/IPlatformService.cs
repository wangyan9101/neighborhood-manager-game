namespace NeighborhoodManager.Platform
{
    public interface IPlatformService
    {
        string PlatformName { get; }
        bool IsWeb { get; }
        bool IsMobile { get; }
    }
}
