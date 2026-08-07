using System;
using System.Threading;
using Top.Assets.Conversion.Pipeline;
using UnityEditor;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// The editor's cancelable progress bar as the pipeline sees it: a
    /// progress sink going one way, a cancellation token coming back.
    /// </summary>
    internal class EditorProgress : IProgress<ConversionProgress>, IDisposable
    {
        private const string Title = "Top Importer";

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        internal CancellationToken Token => _cancellation.Token;

        internal bool Canceled => _cancellation.IsCancellationRequested;

        public void Report(ConversionProgress value)
        {
            if (EditorUtility.DisplayCancelableProgressBar(Title, value.Unit, value.Fraction))
            {
                _cancellation.Cancel();
            }
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
            _cancellation.Dispose();
        }
    }
}
