using EvilsoftCommons.SingleInstance;

namespace IAGrim.Tests.EvilsoftCommons.SingleInstance;

public sealed class SingleInstanceTests {
    [Fact]
    public void SuccessiveInstanceNotifiesFirstInstance() {
        var identifier = Guid.NewGuid();
        using var notification = new ManualResetEventSlim();
        using var firstInstance = new global::EvilsoftCommons.SingleInstance.SingleInstance(identifier);
        firstInstance.ListenForSuccessiveInstances(notification.Set);

        using var successiveInstance = new global::EvilsoftCommons.SingleInstance.SingleInstance(identifier);

        Assert.False(successiveInstance.IsFirstInstance);
        Assert.True(successiveInstance.NotifyFirstInstance());
        Assert.True(notification.Wait(TimeSpan.FromSeconds(2)));
    }
}
