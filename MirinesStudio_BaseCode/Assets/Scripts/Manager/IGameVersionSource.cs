using Cysharp.Threading.Tasks;

namespace MirinesStudio
{
    public interface IGameVersionSource
    {
        bool IsReady { get; }
        UniTask<string> FetchVersionPassAsync();
    }
}
