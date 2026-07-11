namespace NeighborhoodManager.Platform
{
    public sealed class DefaultPlatformService : IPlatformService
    {
        public string PlatformName
        {
            get
            {
#if UNITY_WEBGL
                return "WebGL";
#elif UNITY_ANDROID
                return "Android";
#elif UNITY_IOS
                return "iOS";
#elif UNITY_STANDALONE_WIN
                return "Windows";
#else
                return "Editor/Other";
#endif
            }
        }

        public bool IsWeb
        {
            get
            {
#if UNITY_WEBGL
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsMobile
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }
    }
}
