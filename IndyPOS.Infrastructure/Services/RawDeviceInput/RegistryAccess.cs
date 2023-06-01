using Microsoft.Win32;
using System.Runtime.Versioning;

namespace IndyPOS.Infrastructure.Services.RawDeviceInput;

[type: SupportedOSPlatform("windows")]
internal static class RegistryAccess
{
	internal static RegistryKey? GetDeviceKey(string device)
	{
		var split = device[4..].Split('#');

		var classCode = split[0];
		var subClassCode = split[1];
		var protocolCode = split[2];

		return Registry.LocalMachine.OpenSubKey($@"System\CurrentControlSet\Enum\{classCode}\{subClassCode}\{protocolCode}");
	}

	internal static string GetClassType(string classGuid)
	{
		var classGuidKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\" + classGuid);

		return classGuidKey?.GetValue("Class") as string ?? string.Empty;
	}
}