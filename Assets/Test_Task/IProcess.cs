using System.Threading;
using Cysharp.Threading.Tasks;

namespace State {


    public interface IProcess {

        void Prepare();

        UniTask RunAsync(CancellationToken token);
    }

}