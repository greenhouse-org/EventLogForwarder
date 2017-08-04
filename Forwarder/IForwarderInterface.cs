using System.Diagnostics;

namespace Forwarder
{
    public interface IForwarderInterface
    {
        void Write(EventLogEntry entry);
    }
}
