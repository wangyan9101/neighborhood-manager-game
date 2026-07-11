namespace NeighborhoodManager.Utilities
{
    public interface IRandomSource
    {
        int Range(int minimumInclusive, int maximumExclusive);
        float Range(float minimumInclusive, float maximumInclusive);
    }
}
