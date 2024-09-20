// <copyright file="GpuOption.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>
#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MaaWpfGui.Constants;
using MaaWpfGui.Extensions;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Devices.Display;
using Windows.Win32.Devices.Properties;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;

namespace MaaWpfGui.Helper
{
    public abstract class GpuOption
    {
        public static GpuOption Disable => DisableOption.Instance;

        private static readonly List<GpuOption> _unavailableOptions = [Disable];

        public static bool AllowDeprecatedGpu
        {
            get
            {
                _ = bool.TryParse(ConfigurationHelper.GetValue(ConfigurationKeys.PerformanceAllowDeprecatedGpu, "false"), out var allowUnsupported);
                return allowUnsupported;
            }

            set
            {
                ConfigurationHelper.SetValue(ConfigurationKeys.PerformanceAllowDeprecatedGpu, value.ToString());
            }
        }

        // use string literal to efficiently store uint16 array
        // https://github.com/torvalds/linux/blob/v6.10/drivers/gpu/drm/amd/amdgpu/amdgpu_drv.c#L1756
        private static ReadOnlySpan<ushort> _amd_blacklist => MemoryMarshal.Cast<char, ushort>(
            /* CHIP_TAHITI   */ "\u6780\u6784\u6788\u678A\u6790\u6791\u6792\u6798\u6799\u679A\u679B\u679E\u679F" +
            /* CHIP_PITCAIRN */ "\u6800\u6801\u6802\u6806\u6808\u6809\u6810\u6811\u6816\u6817\u6818\u6819" +
            /* CHIP_OLAND    */ "\u6600\u6601\u6602\u6603\u6604\u6605\u6606\u6607\u6608" +
            "\u6610\u6611\u6613\u6617\u6620\u6621\u6623\u6631" +
            /* CHIP_VERDE    */ "\u6820\u6821\u6822\u6823\u6824\u6825\u6826\u6827\u6828\u6829\u682A\u682B\u682C" +
            "\u682D\u682F\u6830\u6831\u6835\u6837\u6838\u6839\u683B\u683D\u683F" +
            /* CHIP_HAINAN   */ "\u6660\u6663\u6664\u6665\u6667\u666F" +
            /* CHIP_KAVERI   */ "\u1304\u1305\u1306\u1307\u1309\u130A\u130B\u130C\u130D\u130E\u130F" +
            "\u1310\u1311\u1312\u1313\u1315\u1316\u1317\u1318\u131B\u131C\u131D" +
            /* CHIP_BONAIRE  */ "\u6640\u6641\u6646\u6647\u6649\u6650\u6651\u6658\u665c\u665d\u665f" +
            /* CHIP_HAWAII   */ "\u67A0\u67A1\u67A2\u67A8\u67A9\u67AA\u67B0\u67B1\u67B8\u67B9\u67BA\u67BE" +
            /* CHIP_KABINI   */ "\u9830\u9831\u9832\u9833\u9834\u9835\u9836\u9837" +
            "\u9838\u9839\u983a\u983b\u983c\u983d\u983e\u983f" +
            /* CHIP_MULLINS  */ "\u9850\u9851\u9852\u9853\u9854\u9855\u9856\u9857" +
            "\u9858\u9859\u985A\u985B\u985C\u985D\u985E\u985F" +
            /* CHIP_TOPAZ    */ "\u6900\u6901\u6902\u6903\u6907" +
            /* CHIP_TONGA    */ "\u6920\u6921\u6928\u6929\u692B\u692F\u6930\u6938\u6939" +
            /* CHIP_FIJI     */ "\u7300\u730F" +
            /* CHIP_CARRIZO  */ "\u9870\u9874\u9875\u9876\u9877" +
            /* CHIP_STONEY   */ "\u98E4");

        // https://dgpu-docs.intel.com/devices/hardware-table.html#gpus-with-unsupported-drivers
        private static ReadOnlySpan<ushort> _intel_blacklist => MemoryMarshal.Cast<char, ushort>(
            /* Gen11 */ "\u8A70\u8A71\u8A56\u8A58\u8A5B\u8A5D\u8A54\u8A5A\u8A5C\u8A57\u8A59\u8A50\u8A51\u8A52\u8A53" +
            /* Gen9  */ "\u3EA5\u3EA8\u3EA6\u3EA7\u3EA2\u3E90\u3E93\u3E99\u3E9C\u3EA1\u9BA5\u9BA8\u3EA4\u9B21\u9BA0" +
            "\u9BA2\u9BA4\u9BAA\u9BAB\u9BAC\u87CA\u3EA3\u9B41\u9BC0\u9BC2\u9BC4\u9BCA\u9BCB\u9BCC\u3E91\u3E92\u3E98" +
            "\u3E9B\u9BC5\u9BC8\u3E96\u3E9A\u3E94\u9BC6\u9BE6\u9BF6\u3EA9\u3EA0\u593B\u5923\u5926\u5927\u5917\u5912" +
            "\u591B\u5916\u5921\u591A\u591D\u591E\u591C\u87C0\u5913\u5915\u5902\u5906\u590B\u590A\u5908\u590E\u3185" +
            "\u3184\u1A85\u5A85\u0A84\u1A84\u5A84\u192A\u1932\u193B\u193A\u193D\u1923\u1926\u1927\u192B\u192D\u1912" +
            "\u191B\u1913\u1915\u1917\u191A\u1916\u1921\u191D\u191E\u1902\u1906\u190B\u190A\u190E");

        // https://download.nvidia.com/XFree86/Linux-x86_64/555.58/README/supportedchips.html
        private static ReadOnlySpan<ushort> _nvidia_blacklist => MemoryMarshal.Cast<char, ushort>(
            /* Kepler */ "\u0FC6\u0FC8\u0FC9\u0FCD\u0FCE\u0FD1\u0FD2\u0FD3\u0FD4\u0FD5\u0FD8\u0FD9\u0FDF\u0FE0\u0FE1" +
            "\u0FE2\u0FE3\u0FE4\u0FE9\u0FEA\u0FEC\u0FED\u0FEE\u0FF6\u0FF8\u0FF9\u0FFA\u0FFB\u0FFC\u0FFD\u0FFE\u0FFF" +
            "\u1001\u1004\u1005\u1007\u1008\u100A\u100C\u1021\u1022\u1023\u1024\u1026\u1027\u1028\u1029\u102A\u102D" +
            "\u103A\u103C\u1180\u1183\u1184\u1185\u1185\u1187\u1188\u1189\u1189\u118A\u118E\u118F\u1193\u1194\u1195" +
            "\u1198\u1199\u1199\u119A\u119D\u119E\u119F\u11A0\u11A1\u11A2\u11A3\u11A7\u11B4\u11B6\u11B7\u11B8\u11BA" +
            "\u11BC\u11BD\u11BE\u11C0\u11C2\u11C3\u11C4\u11C5\u11C6\u11C8\u11CB\u11E0\u11E1\u11E2\u11E3\u11E3\u11FA" +
            "\u11FC\u1280\u1281\u1282\u1284\u1286\u1287\u1288\u1289\u128B\u1290\u1290\u1291\u1292\u1292\u1293\u1295" +
            "\u1295\u1296\u1298\u1299\u1299\u129A\u12B9\u12BA" +
            /* Maxwell */ "\u1340\u1341\u1344\u1346\u1347\u1348\u1349\u134B\u134D\u134E\u134F\u137A\u137B\u1380" +
            "\u1381\u1382\u1390\u1391\u1392\u1393\u1398\u1399\u139A\u139B\u139C\u139D\u13B0\u13B1\u13B2\u13B3\u13B4" +
            "\u13B6\u13B9\u13BA\u13BB\u13BC\u13C0\u13C2\u13D7\u13D8\u13D9\u13DA\u13F0\u13F1\u13F2\u13F3\u13F8\u13F9" +
            "\u13FA\u13FB\u1401\u1402\u1406\u1407\u1427\u1430\u1431\u1436\u1617\u1618\u1619\u161A\u1667\u174D\u174E" +
            "\u179C\u17C2\u17C8\u17F0\u17F1\u17FD");

        private static IDXGIFactory4? _factory;

        private static IDXGIFactory4? GetDxgiFactory()
        {
            if (_factory != null)
            {
                try
                {
                    if (!_factory.IsCurrent())
                    {
                        _factory = null;
                    }
                    else
                    {
                        return _factory;
                    }
                }
                catch (Exception)
                {
                    _factory = null;
                }
            }

            var hr = PInvoke.CreateDXGIFactory2(0, typeof(IDXGIFactory4).GUID, out var comobj);
            if (hr.Failed)
            {
                return null;
            }

            _factory = (IDXGIFactory4)comobj;
            return _factory;
        }

        public static List<GpuOption> GetGpuOptions()
        {
            var factory = GetDxgiFactory();

            if (factory == null)
            {
                return _unavailableOptions;
            }

            uint i = 0;

            var options = new List<GpuOption> { Disable };

            while (true)
            {
                IDXGIAdapter1 adapter;
                var hr = factory.EnumAdapters1(i, out adapter);
                if (hr == HRESULT.DXGI_ERROR_NOT_FOUND)
                {
                    break;
                }

                var index = i;
                i++;

                var desc = adapter.GetDesc1();
                var instance_path = GetAdapterInstancePath(desc.AdapterLuid);
                var driverInfo = GetGpuDriverInformation(desc.Description.ToString(), instance_path);

                if (!CheckGpu(adapter, ref desc, instance_path, driverInfo, out var deprecated))
                {
                    continue;
                }

                if (index == 0)
                {
                    options.Add(new SystemDefaultOption(driverInfo) { IsDeprecated = deprecated });
                }

                var opt = new SpecificGpuOption(index, desc, instance_path ?? string.Empty, driverInfo) { IsDeprecated = deprecated };

                options.Add(opt);
            }

            if (i == 0)
            {
                return _unavailableOptions;
            }

            // show index if multiple adapters with same name installed
            var counter = new Dictionary<string, int>();
            foreach (var option in options.OfType<SpecificGpuOption>())
            {
                counter[option.GpuInfo.Description] = counter.GetValueOrDefault(option.GpuInfo.Description, 0) + 1;
            }

            foreach (var option in options.OfType<SpecificGpuOption>())
            {
                if (counter.GetValueOrDefault(option.GpuInfo.Description, 0) > 1)
                {
                    option.ShowIndex = true;
                }
            }

            return options;
        }

        private static bool CheckD3D12Support(IDXGIAdapter1 adapter, bool requireFL12 = false)
        {
            // using the same feature level as onnxruntime does
            var hr = D3D12CreateDevice(adapter, requireFL12 ? D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0 : D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0, typeof(ID3D12Device).GUID, 0);

            return hr == HRESULT.S_FALSE;

            [DllImport("d3d12.dll", ExactSpelling = true)]
            static extern HRESULT D3D12CreateDevice([MarshalAs(UnmanagedType.IUnknown)] object pAdapter, D3D_FEATURE_LEVEL MinimumFeatureLevel, in Guid riid, nint ppDevice);
        }

        private static unsafe string? GetAdapterInstancePath(LUID luid)
        {
            try
            {
                var req = new DISPLAYCONFIG_ADAPTER_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        size = (uint)sizeof(DISPLAYCONFIG_ADAPTER_NAME),
                        adapterId = luid,
                        id = 0,
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME,
                    },
                };
                var adpname = PInvoke.DisplayConfigGetDeviceInfo(ref req.header);

                var interface_path = req.adapterDevicePath.ToString();
                uint size = 0;
                var err = PInvoke.CM_Get_Device_Interface_Property(interface_path, PInvoke.DEVPKEY_Device_InstanceId, out var type, null, ref size, 0);

                if (err != CONFIGRET.CR_BUFFER_SMALL)
                {
                    return null;
                }

                if (type != DEVPROPTYPE.DEVPROP_TYPE_STRING)
                {
                    return null;
                }

                var buf = ArrayPool<byte>.Shared.Rent((int)size);
                string? result;

                fixed (byte* ptr = buf)
                {
                    err = PInvoke.CM_Get_Device_Interface_Property(interface_path, PInvoke.DEVPKEY_Device_InstanceId, out _, ptr, ref size, 0);
                    if (err != CONFIGRET.CR_SUCCESS)
                    {
                        return null;
                    }

                    var cch = (int)(size / 2) - 1;
                    result = Marshal.PtrToStringUni((nint)ptr, cch);
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsIndirectDisplayAdapter(string instance_path)
        {
            try
            {
                using var regkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + instance_path, false);

                var upperfilters = regkey?.GetValue("UpperFilters");

                if (upperfilters == null)
                {
                    return false;
                }

                if (upperfilters is string[] sa)
                {
                    return sa.Any(s => s.Equals("IndirectKmd", StringComparison.InvariantCultureIgnoreCase));
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static unsafe GpuDriverInformation GetGpuDriverInformation(string description, string? instance_path)
        {
            if (instance_path == null)
            {
                return new(description, null, null);
            }

            uint devinst;

            fixed (char* ptr = instance_path)
            {
                var err2 = PInvoke.CM_Locate_DevNode(out devinst, ptr, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL);

                if (err2 != CONFIGRET.CR_SUCCESS)
                {
                    return new(description, null, null);
                }
            }

            System.Runtime.InteropServices.ComTypes.FILETIME ft;
            uint size = (uint)sizeof(System.Runtime.InteropServices.ComTypes.FILETIME);

            var err = PInvoke.CM_Get_DevNode_Property(devinst, PInvoke.DEVPKEY_Device_DriverDate, out _, (byte*)(&ft), ref size, 0);

            if (err != CONFIGRET.CR_SUCCESS)
            {
                return new(description, null, null);
            }

            var driverDate = ft.ToDateTime().Date;

            size = 0;
            err = PInvoke.CM_Get_DevNode_Property(devinst, PInvoke.DEVPKEY_Device_DriverVersion, out _, null, ref size, 0);

            if (err != CONFIGRET.CR_BUFFER_SMALL)
            {
                return new(description, null, driverDate);
            }

            var buf = new byte[size];
            string? driverVersion;
            fixed (byte* ptr = buf)
            {
                PInvoke.CM_Get_DevNode_Property(devinst, PInvoke.DEVPKEY_Device_DriverVersion, out _, ptr, ref size, 0);
                driverVersion = Marshal.PtrToStringUni((nint)ptr);
            }

            return new(description, driverVersion, driverDate);
        }

        private static bool CheckGpu(IDXGIAdapter1 adapter, ref DXGI_ADAPTER_DESC1 desc, string? instance_path, GpuDriverInformation driverInfo, out bool deprecated)
        {
            deprecated = false;
            if (((uint)desc.Flags & (uint)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
            {
                // skip software device
                return false;
            }

            if (!CheckD3D12Support(adapter, requireFL12: false))
            {
                // skip adapters without D3D12 support
                // onnxruntime uses FL 11_0 for device creation
                return false;
            }

            if (instance_path != null && IsIndirectDisplayAdapter(instance_path))
            {
                // skip virtual adapters (streaming/RDP)
                return false;
            }

            // check for deprecated GPU
            bool IsGpuDeprecated(ref DXGI_ADAPTER_DESC1 desc)
            {
                // reject GPUs without native D3D12 support (FL 12_0)
                if (!CheckD3D12Support(adapter, requireFL12: true))
                {
                    return true;
                }

                // reject drivers that predates DirectML (released alongside with Windows 10 1903, i.e. 2019-05-21)
                if (instance_path != null)
                {
                    var devdate = driverInfo.DriverDate;
                    if (devdate.HasValue && devdate.Value < new DateTime(2019, 5, 21))
                    {
                        return true;
                    }
                }

                // reject legacy GPUs
                // since we filtered GPUs without FL 12_0, only a few generations will be listed here
                // Intel: pre-Xe (Gen9, Gen11)
                // AMD: pre-Polaris (Sea Islands, Volcanic Islands, Arctic Islands)
                // NVIDIA: maybe pre-Pascal? (Kepler, Maxwell)
                ReadOnlySpan<ushort> devid_blacklist = desc.VendorId switch
                {
                    0x8086 => _intel_blacklist,
                    0x1002 => _amd_blacklist,
                    0x10de => _nvidia_blacklist,
                    _ => default,
                };

                return devid_blacklist.Contains((ushort)desc.DeviceId);
            }

            deprecated = IsGpuDeprecated(ref desc);

            return true;
        }

        public static GpuOption GetCurrent()
        {
            bool.TryParse(ConfigurationHelper.GetValue(ConfigurationKeys.PerformanceUseGpu, "false"), out var useGpu);
            var preferredGpuInstancePath = ConfigurationHelper.GetValue(ConfigurationKeys.PerformancePreferredGpuInstancePath, string.Empty);
            var preferredGpuDescription = ConfigurationHelper.GetValue(ConfigurationKeys.PerformancePreferredGpuDescription, string.Empty);

            GpuOption result;
            if (useGpu)
            {
                var options = GetGpuOptions();
                if (ReferenceEquals(options, _unavailableOptions))
                {
                    result = Disable;
                }

                result = options.OfType<SystemDefaultOption>().FirstOrDefault(Disable);
                if (preferredGpuInstancePath != string.Empty)
                {
                    result = options.OfType<SpecificGpuOption>().FirstOrDefault(x => string.Equals(((SpecificGpuOption)x).InstancePath, preferredGpuInstancePath, StringComparison.Ordinal), result);
                }

                if (result is SystemDefaultOption && preferredGpuDescription != string.Empty)
                {
                    // instance path lookup failed, fallback to description (name) lookup
                    result = options.OfType<SpecificGpuOption>().FirstOrDefault(x => string.Equals(((SpecificGpuOption)x).GpuInfo.Description, preferredGpuDescription, StringComparison.OrdinalIgnoreCase), result);
                }
            }
            else
            {
                result = Disable;
            }

            return result;
        }

        public static void SetCurrent(GpuOption option)
        {
            switch (option)
            {
                case DisableOption _:
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformanceUseGpu, "false");
                    break;
                case SystemDefaultOption _:
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformanceUseGpu, "true");
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformancePreferredGpuDescription, string.Empty);
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformancePreferredGpuInstancePath, string.Empty);
                    break;
                case SpecificGpuOption x:
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformanceUseGpu, "true");
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformancePreferredGpuDescription, x.GpuInfo.Description);
                    ConfigurationHelper.SetValue(ConfigurationKeys.PerformancePreferredGpuInstancePath, x.InstancePath);
                    break;
                default:
                    break;
            }
        }

        public record GpuDriverInformation(string Description, string? DriverVersion, DateTime? DriverDate);

        public class DisableOption : GpuOption
        {
            public static DisableOption Instance { get; } = new DisableOption();

            private DisableOption()
            {
            }

            // make WPF happy
            public bool IsDeprecated => false;

            public override bool Equals(object? obj)
            {
                return obj is DisableOption;
            }

            public override int GetHashCode() => typeof(DisableOption).GetHashCode();

            public override string ToString() => LocalizationHelper.GetString("GpuOptionDisable");
        }

        public abstract class EnableOption : GpuOption
        {
            public abstract uint Index { get; }

            public bool IsDeprecated { get; set; }

            public abstract GpuDriverInformation GpuInfo { get; }
        }

        public class SystemDefaultOption : EnableOption
        {
            public override uint Index => 0;

            public override GpuDriverInformation GpuInfo { get; }

            public SystemDefaultOption(GpuDriverInformation info)
            {
                GpuInfo = info;
            }

            public override bool Equals(object? obj)
            {
                return obj is SystemDefaultOption;
            }

            public override int GetHashCode() => typeof(SystemDefaultOption).GetHashCode();

            public override string ToString() => LocalizationHelper.GetString("GpuOptionSystemDefault") + (GpuInfo.Description == null ? string.Empty : $" ({GpuInfo.Description})");
        }

        public class SpecificGpuOption : EnableOption
        {
            private DXGI_ADAPTER_DESC1 _description;
            private uint _index;
            private string _instance_path;

            public override GpuDriverInformation GpuInfo { get; }

            public bool ShowIndex { get; set; }

            internal SpecificGpuOption(uint index, DXGI_ADAPTER_DESC1 description, string instance_path, GpuDriverInformation info)
            {
                _index = index;
                _description = description;
                _instance_path = instance_path;
                GpuInfo = info;
            }

            public override uint Index => _index;

            public string InstancePath => _instance_path;

            public override bool Equals(object? obj)
            {
                if (obj is SpecificGpuOption x)
                {
                    return x._description.AdapterLuid.AsLong() == _description.AdapterLuid.AsLong();
                }

                return false;
            }

            public override int GetHashCode() => (typeof(SpecificGpuOption), _description, _index, _instance_path).GetHashCode();

            public override string ToString() => ShowIndex ? _description + $" (GPU {_index})" : _description.Description.ToString();
        }
    }
}
