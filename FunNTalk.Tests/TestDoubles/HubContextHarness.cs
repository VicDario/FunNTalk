using FunNTalk.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace FunNTalk.Tests.TestDoubles;

/// <summary>
/// Wires up a substituted <see cref="IHubContext{THub}"/> for <see cref="CommunicationHub"/>.
///
/// The <c>SendAsync</c> overloads handlers call are extension methods, and NSubstitute cannot
/// intercept those. Every assertion therefore targets the underlying virtual
/// <see cref="IClientProxy.SendCoreAsync"/>, which is what the extensions funnel into.
/// </summary>
internal sealed class HubContextHarness
{
    public HubContextHarness()
    {
        HubContext = Substitute.For<IHubContext<CommunicationHub>>();
        Clients = Substitute.For<IHubClients>();
        Groups = Substitute.For<IGroupManager>();

        HubContext.Clients.Returns(Clients);
        HubContext.Groups.Returns(Groups);
    }

    public IHubContext<CommunicationHub> HubContext { get; }

    public IHubClients Clients { get; }

    public IGroupManager Groups { get; }

    /// <summary>Proxy returned for <c>Clients.Group(roomName)</c>.</summary>
    public IClientProxy StubGroup(string roomName)
    {
        var proxy = Substitute.For<IClientProxy>();
        Clients.Group(roomName).Returns(proxy);
        return proxy;
    }

    /// <summary>
    /// Proxy returned for <c>Clients.GroupExcept(roomName, connectionId)</c>. That two-string form
    /// is itself an extension over <c>GroupExcept(string, IReadOnlyList&lt;string&gt;)</c>, so the
    /// list overload is the one that gets stubbed.
    /// </summary>
    public IClientProxy StubGroupExcept(string roomName)
    {
        var proxy = Substitute.For<IClientProxy>();
        Clients.GroupExcept(roomName, Arg.Any<IReadOnlyList<string>>()).Returns(proxy);
        return proxy;
    }

    /// <summary>
    /// Proxy returned for <c>Clients.Client(connectionId)</c>. <see cref="IHubClients"/> redeclares
    /// <c>Client</c> with a narrower return type, so both slots are stubbed — the base one first,
    /// because the redeclaration may delegate to it.
    /// </summary>
    public ISingleClientProxy StubClient(string connectionId)
    {
        var proxy = Substitute.For<ISingleClientProxy>();
        ((IHubClients<IClientProxy>)Clients).Client(connectionId).Returns(proxy);
        Clients.Client(connectionId).Returns(proxy);
        return proxy;
    }
}
