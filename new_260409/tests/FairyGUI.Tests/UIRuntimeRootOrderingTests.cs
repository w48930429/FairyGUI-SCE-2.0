using System.Drawing;
using System.Reflection;
using FairyGUI;
using FairyGUI.Render;
using Xunit;

namespace FairyGUI.Tests;

public class UIRuntimeRootOrderingTests
{
    [Fact]
    public void Show_ReopensWindowByRemountingItsNativeRoot()
    {
        var mountTracker = RootMountRecordingAdapter.Create();
        UIRuntime.Adapter = mountTracker.Adapter;
        var window = new Window();

        try
        {
            window.Show();
            window.Show();

            Assert.True(window.IsShowing);
            Assert.NotNull(window.NativeObject);
            Assert.Equal([window.NativeObject, window.NativeObject], mountTracker.Recorder.FixedSizeRootMounts);
        }
        finally
        {
            window.HideImmediately();
        }
    }

    [Fact]
    public void AddToFullScreenRoot_ReopensExistingContentByRemountingItsRoot()
    {
        var mountTracker = RootMountRecordingAdapter.Create();
        UIRuntime.Adapter = mountTracker.Adapter;
        var content = new GComponent();

        try
        {
            var root = UIRuntime.AddToFullScreenRoot(content);
            var reopenedRoot = UIRuntime.AddToFullScreenRoot(content);

            Assert.Same(root, reopenedRoot);
            Assert.NotNull(root.NativeObject);
            Assert.Equal([root.NativeObject, root.NativeObject], mountTracker.Recorder.RootMounts);
        }
        finally
        {
            UIRuntime.RemoveFromRoot(content, dispose: true);
        }
    }
    [Fact]
    public void RemoveFromRoot_DisposesFullScreenContentAndUnregistersItsRoot()
    {
        var mountTracker = RootMountRecordingAdapter.Create();
        UIRuntime.Adapter = mountTracker.Adapter;
        var content = new GComponent();
        var root = UIRuntime.AddToFullScreenRoot(content);

        UIRuntime.RemoveFromRoot(content, dispose: true);

        Assert.False(UIRuntime.IsFullScreenContent(content));
        Assert.DoesNotContain(root, UIRuntime.GetTopLevelObjectsSnapshot());
    }

    private class RootMountRecordingAdapter : DispatchProxy
    {
        public List<object> RootMounts { get; } = [];
        public List<object> FixedSizeRootMounts { get; } = [];

        public static (ISCEAdapter Adapter, RootMountRecordingAdapter Recorder) Create()
        {
            var adapter = DispatchProxy.Create<ISCEAdapter, RootMountRecordingAdapter>();
            return (adapter, (RootMountRecordingAdapter)(object)adapter);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case nameof(ISCEAdapter.CreatePanel):
                    return new object();
                case nameof(ISCEAdapter.AddToRoot):
                    RootMounts.Add((object)args![0]!);
                    break;
                case nameof(ISCEAdapter.AddToRootWithFixedSize):
                    FixedSizeRootMounts.Add((object)args![0]!);
                    break;
                case nameof(ISCEAdapter.GetScreenSize):
                    return new SizeF(1136f, 640f);
            }

            return targetMethod.ReturnType == typeof(void)
                ? null
                : targetMethod.ReturnType.IsValueType
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
        }
    }
}
