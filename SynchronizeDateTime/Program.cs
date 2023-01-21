using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;

namespace SynchronizeDateTime
{
    public class Program
    {
        public struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        };

        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        private extern static bool Win32SetSystemTime(ref SystemTime sysTime);
        public static void Main(string[] args)
        {
            DateTime now = GetNetworkTime();
            DateTime dateTime = now;
            
            SystemTime updatedTime = new SystemTime();
            updatedTime.Year = (ushort)dateTime.Year;
            updatedTime.Month = (ushort)dateTime.Month;
            updatedTime.Day = (ushort)dateTime.Day;
            updatedTime.Hour = (ushort)dateTime.Hour;
            updatedTime.Minute = (ushort)dateTime.Minute;
            updatedTime.Second = (ushort)dateTime.Second;
            // Call the unmanaged function that sets the new date and time instantly
            Win32SetSystemTime(ref updatedTime);
        }

        public static DateTime GetNetworkTime()
        {
            const string ntpServer = "pool.ntp.org";
            var ntpData = new byte[48];
            ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }
    }
}